using UnityEngine;

[RequireComponent(typeof(InventoryItemView))]
[RequireComponent(typeof(InventoryItemPlacement))]
[RequireComponent(typeof(InventoryItemDragHandler))]
public class InventoryItemController : MonoBehaviour, IPoolable
{
    [Header("Item")]
    [SerializeField] private ItemType itemType;
    [SerializeField] private PoolKey poolKey;
    [SerializeField] private int startLevel = 0;

    public PoolKey PoolKey => poolKey;
    public InventoryItemModel Model => model;
    public InventoryItemPlacement Placement => placement;
    public InventoryItemView View => view;

    private InventoryItemModel model;
    private InventoryItemView view;
    private InventoryItemPlacement placement;
    private InventoryItemDragHandler dragHandler;

    private void Awake()
    {
        view = GetComponent<InventoryItemView>();
        placement = GetComponent<InventoryItemPlacement>();
        dragHandler = GetComponent<InventoryItemDragHandler>();
    }

    public void OnSpawn()
    {
        BuildModel();
        BindComponents();
        gameObject.SetActive(true);
    }

    public void OnDespawn()
    {
        gameObject.SetActive(false);
    }

    private void BuildModel()
    {
        var db = GameManager.Instance.ItemDatabase;
        model = new InventoryItemModel(itemType, startLevel, db);
    }

    private void BindComponents()
    {
        placement.Initialize(model);
        view.Initialize(model);

        dragHandler.Initialize(this, model, placement);
    }

    public void SetLevel(int newLevel)
    {
        model.SetLevel(newLevel);
        view.Initialize(model);
    }
}
