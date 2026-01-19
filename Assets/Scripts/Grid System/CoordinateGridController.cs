using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Koordinat Bazlı Grid Kontrolcüsü (Tetris-Style)
/// Öğelerin şekline göre hücreleri kaplayan, klasik envanter grid sistemi.
/// </summary>
public class CoordinateGridController : BaseGridController
{
    // Static buffer (GC allocation önleme - GetTopLeftWorld için)
    private static readonly Vector3[] WorldCornersBuffer = new Vector3[4];

    [Header("---- Layout Cache (Performans Optimizasyonu) ----")]
    private RectTransform gridRectTransform; // Grid'in RectTransform bileşeni
    private bool layoutCacheDirty = true; // Layout cache'in güncellenmeye ihtiyacı var mı?
    private bool layoutCacheValid; // Layout cache geçerli mi?
    private Vector2 cachedTopLeft00Local; // (0,0) hücresinin sol-üst köşesinin local koordinatları
    private float cachedStepX; // Hücreler arası yatay adım mesafesi
    private float cachedStepY; // Hücreler arası dikey adım mesafesi
    private float cachedInvStepX; // Inverse of stepX
    private float cachedInvStepY; // Inverse of stepY

    [Header("---- Grid Settings ----")]
    [Min(1)][SerializeField] private int columns = 10; // Sütun sayısı
    public int Columns => columns;
    [Min(1)][SerializeField] private int rows = 5; // Satır sayısı
    public int Rows => rows;

    [Header("---- UI ----")]
    [SerializeField] private GridLayoutGroup gridLayout; // Unity GridLayoutGroup bileşeni
    [Tooltip("Hücreler bu transform altında tutulur. Null ise bu transform kullanılır.")]
    [SerializeField] private Transform cellsRoot;


    [Header("---- Doluluk ----")]
    private InventoryItemController[,] occupied; // Her hücrede hangi öğe var
    private readonly Dictionary<Vector2Int, GridCellController> cellsByCoord = new(); // Koordinattan hücre nesnesine erişim
    private readonly List<GridCellController> previewCells = new();  // Önizleme için renklendirilmiş hücreler listesi


    protected override void OnEnable()
    {
        base.OnEnable(); // BaseGridController'ın kaydını yap
        // Koordinat grid'e özel işlemler buraya eklenebilir
    }

    protected override void OnDisable()
    {
        base.OnDisable(); // BaseGridController'ın kaydını sil
        // Koordinat grid'e özel temizlik buraya eklenebilir
    }

    // Layout Metriklerini Al
    // Grid'in hücre boyutu ve boşluk değerlerini döndürür.
    // GridLayoutGroup varsa ondan alır, yoksa varsayılan değerler kullanır.
    public void GetLayoutMetrics(out Vector2 cellSize, out Vector2 spacing)
    {
        if (gridLayout != null)
        {
            cellSize = gridLayout.cellSize;
            spacing = gridLayout.spacing;
            return;
        }

        // defaults
        cellSize = new Vector2(100, 100);
        spacing = Vector2.zero;
    }

    // GridRectTransform'u Al (BaseGridController override)
    public override RectTransform GetGridRectTransform()
    {
        if (gridRectTransform == null)
        {
            gridRectTransform = gridLayout != null ? gridLayout.GetComponent<RectTransform>() : GetComponent<RectTransform>();
        }
        return gridRectTransform;
    }

    // Ekran Noktasını Hücreye Çevir (BaseGridController override)
    // Ekran koordinatlarını grid hücre koordinatlarına dönüştürür.
    // Grid sınırlarının dışındaki koordinatlar da döndürülebilir (kısmi örtüşme için).
    public override bool TryScreenPointToCell(Vector2 screenPoint, Camera eventCamera, out Vector2Int cell)
    {
        cell = default;

        var rt = GetGridRectTransform();
        if (rt == null) return false;

        if (!RectTransformUtility.RectangleContainsScreenPoint(rt, screenPoint, eventCamera))
            return false;
        cell = default;

        // Layout cache'i güncelle (gerekirse)
        EnsureLayoutCache();
        if (!layoutCacheValid || gridRectTransform == null) return false;

        // Ekran noktasını grid'in local space'ine çevir
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(gridRectTransform, screenPoint, eventCamera, out var local))
            return false;

        // (0,0) hücresinin sol-üst köşesinden delta'yı hesapla
        var delta = local - cachedTopLeft00Local;
        var x = Mathf.FloorToInt(delta.x * cachedInvStepX);
        var y = Mathf.FloorToInt((-delta.y) * cachedInvStepY); // Aşağı yön local space'de negatif

        // NOT: Kasıtlı olarak [0..columns/rows) aralığı dışındaki değerlere de izin veriyoruz
        // böylece önizleme kısmi örtüşmeyi gösterebilir
        cell = new Vector2Int(x, y);
        return true;
    }

    // === Preview Colors ===
    [Header("Preview Colors")]
    [SerializeField] private Color previewValid = new Color(0.2f, 0.9f, 0.2f, 1f);
    [SerializeField] private Color previewBlocked = new Color(0.95f, 0.85f, 0.2f, 1f);
    [SerializeField] private Color previewInvalid = new Color(0.95f, 0.25f, 0.25f, 1f);

    // Grid'in tüm bileşenlerini başlatır: items root, doluluk array'i, hücre koordinatları.
    private void Awake()
    {
        if (cellsRoot == null) cellsRoot = transform;
        if (gridLayout == null) gridLayout = GetComponent<GridLayoutGroup>();
        gridRectTransform = gridLayout != null ? gridLayout.GetComponent<RectTransform>() : GetComponent<RectTransform>();

        EnsureItemsRoot();     // Öğeler için overlay root'u oluştur
        RebuildOccupancy();    // Doluluk array'ini oluştur
        AssignCellCoordinates(); // Hücrelere koordinat ata
        MarkLayoutCacheDirty(); // Layout cache'i güncellenmesi gerektiğini işaretle
    }

    // RectTransform Boyutları Değiştiğinde
    // Grid boyutu değişirse layout cache'i güncellenmesi gerektiğini işaretle.
    private void OnRectTransformDimensionsChange()
    {
        MarkLayoutCacheDirty();
    }

    // Bir sonraki layout hesaplamasında cache'in yenilenmesi gerektiğini belirtir.
    private void MarkLayoutCacheDirty()
    {
        layoutCacheDirty = true;
    }

    // Layout Cache'i Güncelle
    // (0,0) hücresinin pozisyonunu ve hücreler arası adım mesafelerini cache'e alır.
    // Bu sayede her seferinde hesaplama yapmak yerine cache'ten okur (performans).
    private void EnsureLayoutCache()
    {
        if (!layoutCacheDirty) return; // Zaten güncel
        layoutCacheDirty = false;

        gridRectTransform = gridLayout != null ? gridLayout.GetComponent<RectTransform>() : GetComponent<RectTransform>();
        layoutCacheValid = false;
        if (gridRectTransform == null) return;

        // Gerçek hücre transform'larını kullanarak grid orijini ve adım mesafelerini belirle
        // (hizalama, padding vb. karşı sağlam)
        if (!TryGetCellRect(new Vector2Int(0, 0), out var cell00)) return;

        // (0,0) hücresinin sol-üst köşesini local koordinatlara çevir
        cachedTopLeft00Local = (Vector2)gridRectTransform.InverseTransformPoint(GetTopLeftWorld(cell00));

        // Komşu hücrelerden adım boyutlarını hesapla (komşu yoksa layout metriklerine geri dön)
        if (TryGetCellRect(new Vector2Int(1, 0), out var cell10))
        {
            var topLeft10Local = (Vector2)gridRectTransform.InverseTransformPoint(GetTopLeftWorld(cell10));
            cachedStepX = Mathf.Abs(topLeft10Local.x - cachedTopLeft00Local.x);
        }
        else
        {
            // Yedek: layout metriklerinden hesapla
            GetLayoutMetrics(out var cs, out var sp);
            cachedStepX = Mathf.Abs(cs.x + sp.x);
        }

        if (TryGetCellRect(new Vector2Int(0, 1), out var cell01))
        {
            var topLeft01Local = (Vector2)gridRectTransform.InverseTransformPoint(GetTopLeftWorld(cell01));
            cachedStepY = Mathf.Abs(cachedTopLeft00Local.y - topLeft01Local.y);
        }
        else
        {
            // Yedek: layout metriklerinden hesapla
            GetLayoutMetrics(out var cs, out var sp);
            cachedStepY = Mathf.Abs(cs.y + sp.y);
        }

        // Minimum değerler (sıfıra bölme hatası önleme)
        cachedStepX = Mathf.Max(1f, cachedStepX);
        cachedStepY = Mathf.Max(1f, cachedStepY);
        layoutCacheValid = true;

        // Inverse değerleri hesapla
        cachedInvStepX = 1f / cachedStepX;
        cachedInvStepY = 1f / cachedStepY;
    }

    // Items Root'u Oluştur
    // Öğelerin yerleştirileceği overlay root transform'unu oluşturur.
    // Varsa kullanır, yoksa runtime'da grid'in yanına kardeş olarak ekler.
    private void EnsureItemsRoot()
    {
        if (itemsRoot != null) return; // Zaten var
        if (transform.parent == null) return; // Kardeş oluşturamayız

        // Items için yeni GameObject oluştur
        var go = new GameObject($"{name}_ItemsRoot", typeof(RectTransform));
        itemsRoot = go.GetComponent<RectTransform>();
        itemsRoot.SetParent(transform.parent, false);
        itemsRoot.SetSiblingIndex(transform.GetSiblingIndex() + 1); // Grid'in hemen yanına yerleştir

        // Grid'in RectTransform ayarlarını eşleştir (overlay olarak)
        var gridRt = GetComponent<RectTransform>();
        itemsRoot.anchorMin = gridRt.anchorMin;
        itemsRoot.anchorMax = gridRt.anchorMax;
        itemsRoot.pivot = gridRt.pivot;
        // Offset'leri kullan böylece grid genişlese bile eşleşir
        itemsRoot.offsetMin = gridRt.offsetMin;
        itemsRoot.offsetMax = gridRt.offsetMax;
        itemsRoot.localRotation = Quaternion.identity;
        itemsRoot.localScale = Vector3.one;
    }

    // Doluluk Array'ini Yeniden Oluştur
    // Occupied (dolu) 2D array'ini sütun ve satır sayısına göre oluşturur.
    // Her hücre başlangıçta boştur (null).
    private void RebuildOccupancy()
    {
        // Minimum değerleri garanti et
        if (columns < 1) columns = 1;
        if (rows < 1) rows = 1;
        occupied = new InventoryItemController[columns, rows];
    }

    // Hücre Koordinatlarını Ata
    // cellsRoot altındaki tüm GridCellController'lara grid koordinatlarını atar.
    // Sol üstten başlar, satır-sütun düzeninde ilerler.
    private void AssignCellCoordinates()
    {
        if (cellsRoot == null) return;

        cellsByCoord.Clear();
        var index = 0;
        var childCount = cellsRoot.childCount;

        for (int i = 0; i < childCount; i++)
        {
            var child = cellsRoot.GetChild(i);
            var cell = child.GetComponent<GridCellController>();
            if (cell == null) continue; // GridCellController yoksa atla

            // Koordinatları hesapla (satır-sütun düzeni)
            var x = index % columns;
            var y = index / columns;
            var coord = new Vector2Int(x, y);

            // Hücreye grid ve koordinat bilgisini ver
            cell.SetGrid(this, coord);
            cellsByCoord[coord] = cell;
            index++;
        }

        MarkLayoutCacheDirty();
    }

    // Öğeyi Grid'den Kaldır (BaseGridController override)
    // Belirtilen öğenin kapladığı tüm hücreleri boşaltır.
    public override void RemoveItem(InventoryItemController item)
    {
        if (item == null || occupied == null) return;

        // Tüm hücreleri tara ve bu öğeyi temizle
        for (int y = 0; y < rows; y++)
            for (int x = 0; x < columns; x++)
                if (occupied[x, y] == item)
                    occupied[x, y] = null;

        // Öğenin yerleşim bilgisini temizle
        item.Placement.ClearCurrentPlacement(this);
        EventManager.OnItemRemoved?.Invoke(this, item);
    }

    // Öğeyi Yerleştirmeyi Dene
    // Öğeyi Yerleştirmeyi Dene (BaseGridController override)
    // Öğeyi belirtilen çapa hücresine yerleştirmeyi dener.
    // Sınır kontrolü ve çarpışma kontrolü yapar.
    // Başarılı olursa öğeyi hücrelere işler ve görselleştirir.
    /// <returns>Yerleştirme başarılı ise true, değilse false</returns>
    public override bool TryPlaceItem(InventoryItemController item, Vector2Int anchorCell)
    {
        if (item == null) return false;
        if (occupied == null) RebuildOccupancy();
        EnsureItemsRoot();

        item.Model.GetDimensions(out var w, out var h);

        // === Sınır Kontrolü ===
        // Çapa sol-üst köşedir, tüm öğe grid içinde olmalı
        if (anchorCell.x < 0 || anchorCell.y < 0) return false;
        if (anchorCell.x + w > columns) return false;
        if (anchorCell.y + h > rows) return false;

        // === Çarpışma Kontrolü ===
        // Öğenin ayak izindeki her dolu hücre için kontrol et
        for (int ly = 0; ly < h; ly++)
            for (int lx = 0; lx < w; lx++)
            {
                if (!item.Model.IsFilled(lx, ly)) continue; // Boş hücre, atla
                var gx = anchorCell.x + lx;
                var gy = anchorCell.y + ly;
                if (occupied[gx, gy] != null) return false; // Zaten dolu, yerleştiremeyiz
            }

        // === Yerleştirmeyi Onayla ===
        // Öğenin kapladığı hücreleri işaretle
        for (int ly = 0; ly < h; ly++)
            for (int lx = 0; lx < w; lx++)
            {
                if (!item.Model.IsFilled(lx, ly)) continue;
                var gx = anchorCell.x + lx;
                var gy = anchorCell.y + ly;
                occupied[gx, gy] = item;
            }

        // Görsel yerleşimi uygula (pozisyon, boyut, ebeveyn)
        ApplyItemVisual(item, anchorCell, w, h);
        // Öğeye yerleşim bilgisini kaydet
        item.Placement.SetCurrentPlacement(this, anchorCell);
        EventManager.OnItemPlaced?.Invoke(this, item);

        // Event tetikle: Item başka bir grid'den buraya taşındıysa
        // InventoryItem'ın revertGrid'i varsa, bu bir transfer işlemidir
        // Not: InventoryItem'da revertGrid bilgisi var, ama burada direkt kontrol edemiyoruz
        // Bu yüzden event'i InventoryItem'dan tetiklemek daha iyi olabilir
        // Şimdilik burada bırakıyoruz, InventoryItem'da da kontrol edeceğiz

        return true;
    }

    /// <summary>
    /// Grid'deki benzersiz item sayısını döndürür (aynı item birden fazla hücrede olsa bile 1 sayılır)
    /// </summary>
    public int GetUniqueItemCount()
    {
        if (occupied == null) return 0;

        var uniqueItems = new System.Collections.Generic.HashSet<InventoryItemController>();
        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < columns; x++)
            {
                if (occupied[x, y] != null)
                {
                    uniqueItems.Add(occupied[x, y]);
                }
            }
        }
        return uniqueItems.Count;
    }

    /// Önizlemeyi Temizle (BaseGridController override)
    /// Renklendirilmiş önizleme hücrelerini orijinal renklerine döndürür.
    public override void ClearPreview()
    {
        for (int i = 0; i < previewCells.Count; i++)
        {
            if (previewCells[i] != null) previewCells[i].ResetColor();
        }
        previewCells.Clear();
    }

    // Öğe Yerleşimini Önizle (BaseGridController override)
    // Öğe sürüklenirken hücreleri renklendirir (yeşil/sarı/kırmızı).
    // 
    // Kurallar (her hücre için):
    // - Herhangi bir kısım grid dışındaysa: sınır içi boş hücreler => Sarı, sınır içi dolu hücreler => Kırmızı
    // - Tamamen içerde ama herhangi bir çarpışma varsa: sınır içi ayak izi hücreleri => Kırmızı
    // - Tamamen içerde ve boş: sınır içi ayak izi hücreleri => Yeşil
    public override void PreviewItemPlacement(InventoryItemController item, Vector2Int anchorCell)
    {
        if (item == null) return;
        if (occupied == null) RebuildOccupancy();

        ClearPreview();

        item.Model.GetDimensions(out var w, out var h);
        bool anyOutOfBounds = false; // Grid dışında kalan kısım var mı?
        bool anyOccupied = false;    // Dolu hücre var mı?

        // === İlk Geçiş: Global Durum ===
        // Grid dışı veya dolu hücre var mı kontrol et
        for (int ly = 0; ly < h; ly++)
            for (int lx = 0; lx < w; lx++)
            {
                if (!item.Model.IsFilled(lx, ly)) continue;
                var gx = anchorCell.x + lx;
                var gy = anchorCell.y + ly;

                // Grid sınırları dışında mı?
                if (gx < 0 || gy < 0 || gx >= columns || gy >= rows)
                {
                    anyOutOfBounds = true;
                    continue;
                }

                // Hücre dolu mu?
                if (occupied[gx, gy] != null) anyOccupied = true;
            }

        // === İkinci Geçiş: Sınır İçi Her Hücreyi Renklendir ===
        for (int ly = 0; ly < h; ly++)
            for (int lx = 0; lx < w; lx++)
            {
                if (!item.Model.IsFilled(lx, ly)) continue;
                var gx = anchorCell.x + lx;
                var gy = anchorCell.y + ly;

                // Grid dışındaysa atla (renklendirmeyiz)
                if (gx < 0 || gy < 0 || gx >= columns || gy >= rows) continue;

                var coord = new Vector2Int(gx, gy);
                if (!cellsByCoord.TryGetValue(coord, out var cell) || cell == null) continue;

                previewCells.Add(cell);

                // Renk seçimi
                Color color;
                var isOcc = occupied[gx, gy] != null;
                if (anyOutOfBounds)
                {
                    // Kısmi sınır dışı: dolu => kırmızı, boş => sarı
                    color = isOcc ? previewInvalid : previewBlocked;
                }
                else if (anyOccupied)
                {
                    // Tamamen içerde ama çarpışma var: tümü kırmızı
                    color = previewInvalid;
                }
                else
                {
                    // Tamamen içerde ve boş: tümü yeşil
                    color = previewValid;
                }

                cell.SetColor(color);
            }
    }

    /// Öğenin Görselini Uygula
    /// Öğeyi grid üzerinde doğru pozisyon ve boyutta yerleştirir.
    /// RectTransform ayarlarını (pivot, anchor, sizeDelta, position) yapılandırır.
    private void ApplyItemVisual(InventoryItemController item, Vector2Int anchorCell, int w, int h)
    {
        var rt = item.GetComponent<RectTransform>();
        if (rt == null) return;

        // Overlay root'a ebeveynle (layout sisteminin dışında)
        if (itemsRoot != null)
            rt.SetParent(itemsRoot, false);
        else
            rt.SetParent(transform, false);

        // Sol-üst anchor (0,1)
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);

        // Hücre ve boşluk değerlerini al
        var cellSize = gridLayout != null ? gridLayout.cellSize : new Vector2(100, 100);
        var spacing = gridLayout != null ? gridLayout.spacing : Vector2.zero;
        var padding = gridLayout != null ? gridLayout.padding : new RectOffset();

        // Piksel cinsinden genişlik ve yükseklik hesapla
        var pxW = w * cellSize.x + Mathf.Max(0, w - 1) * spacing.x;
        var pxH = h * cellSize.y + Mathf.Max(0, h - 1) * spacing.y;
        rt.sizeDelta = new Vector2(pxW, pxH);

        // Pozisyonu gerçek hücre RectTransform'larından okuyarak ayarla
        // (hizalama/kısıtlama sürprizlerini önler)
        if (TryGetCellRect(anchorCell, out var cellRt))
        {
            var topLeftWorld = GetTopLeftWorld(cellRt);
            var offset = GetOffsetFromTopLeftInParent(itemsRoot != null ? itemsRoot : (RectTransform)transform, topLeftWorld);
            rt.anchoredPosition = new Vector2(offset.x, -offset.y);
        }
        else
        {
            // Yedek matematik (hücreler doğru bağlanmışsa nadiren kullanılır)
            var x = padding.left + anchorCell.x * (cellSize.x + spacing.x);
            var y = -(padding.top + anchorCell.y * (cellSize.y + spacing.y));
            rt.anchoredPosition = new Vector2(x, y);
        }

        rt.localRotation = Quaternion.identity; // Rotasyon ayak izini etkiler, UI rotasyonunu değil
    }

    /// Koordinattan Hücre RectTransform'unu Al
    /// Belirtilen koordinattaki hücrenin RectTransform'unu bulmaya çalışır.
    private bool TryGetCellRect(Vector2Int coord, out RectTransform cellRt)
    {
        cellRt = null;
        if (cellsByCoord.TryGetValue(coord, out var cell) && cell != null)
        {
            cellRt = cell.GetComponent<RectTransform>();
            return cellRt != null;
        }
        return false;
    }

    /// RectTransform'un Sol-Üst Köşesini Dünya Koordinatlarında Al
    private static Vector3 GetTopLeftWorld(RectTransform rt)
    {
        rt.GetWorldCorners(WorldCornersBuffer);
        return WorldCornersBuffer[1]; // Sol-üst köşe
    }

    /// Dünya Noktasından Ebeveyn İçinde Sol-Üst Offseti Hesapla
    /// Bir dünya noktasının, ebeveyn RectTransform içinde sol-üstten ne kadar uzakta olduğunu hesaplar.
    /// <returns>(soldan x, üstten y) ebeveynin rect space'inde</returns>
    private static Vector2 GetOffsetFromTopLeftInParent(RectTransform parent, Vector3 worldPoint)
    {
        var local = (Vector2)parent.InverseTransformPoint(worldPoint);
        var rect = parent.rect;
        var topLeftLocal = new Vector2(-rect.width * parent.pivot.x, rect.height * (1f - parent.pivot.y));

        var diff = local - topLeftLocal;
        return new Vector2(diff.x, -diff.y);
    }
}

