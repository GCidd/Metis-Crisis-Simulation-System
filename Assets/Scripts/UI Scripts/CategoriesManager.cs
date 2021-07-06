using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using UnityEngine.Events;

public class CategoriesManager : MonoBehaviour
{
    [SerializeField]
    private uint defaultCategoryIndex = 0;
    private int currentCategoryIndex;

    private List<GameObject> categoriesButtons = new List<GameObject>();
    private GameObject templateButton;
    private GameObject searchButton;

    private InputField searchInputField;

    private List<string> categoriesNames;
    private Dictionary<string, List<GameObject>> categoryObjects;
    public List<GameObject> GetObjectsOfCategory(string categoryName) { return categoryObjects.ContainsKey(categoryName) ? categoryObjects[categoryName] : new List<GameObject>(); }
    private List<GameObject> categories = new List<GameObject>();
    private List<List<GameObject>> searchableObjects = new List<List<GameObject>>();    // searchable object for each category
    private List<List<GameObject>> allCategoryItemsUI = new List<List<GameObject>>();   // all ui items for each category

    private void Awake()
    {
        categoryObjects = new Dictionary<string, List<GameObject>>();
        templateButton = transform.GetComponentInChildren<ScrollRect>().GetComponentInChildren<Button>().gameObject;

        foreach (Transform button in transform.Find("Categories Buttons"))
        {
            if (button.name.ToLower().Contains("category"))
            {
                categoriesButtons.Add(button.gameObject);
            }
            else if (button.name.ToLower().Contains("search"))
            {
                searchButton = button.gameObject;
            }
        }

        foreach (Transform placeablesCategory in GameObject.Find("Placeables").transform)
        {   // find parents that hold the placeable objects for each category
            if (placeablesCategory.name.ToLower().Contains("placeable"))
            {
                categories.Add(placeablesCategory.gameObject);
            }
        }

        // Order buttons and categories in the same order (according to the buttons' order in ui)
        categoriesNames = categoriesButtons.Select(c => c.name.Split(' ')[0]).ToList();
        foreach (string categoryName in categoriesNames)
        {
            categoryObjects.Add(categoryName, new List<GameObject>());
        }
        categories = categories.OrderBy(c => categoriesNames.IndexOf(c.name.Split(' ')[1])).ToList();

        for (int i = 0; i < categories.Count; i++)
        {
            currentCategoryIndex = i;
            SetupCategoryItems(i);
        }
        templateButton.SetActive(false);

        EnterCategoryOfIndex((int)defaultCategoryIndex);

        searchInputField = GameObject.Find("ObjectSearch").GetComponent<InputField>();

        searchButton.GetComponent<Button>().onClick.AddListener(delegate { OnToggle(); });
        searchButton.GetComponent<ToggleObjectActive>().ToggleObject = GameObject.Find("ObjectSearch");
        searchButton.GetComponent<ToggleObjectActive>().DefaultActiveState = false;
        UnityAction<string> valueChangedEvent = new UnityAction<string>(UpdateVisibleItems);
        searchButton.GetComponent<ToggleObjectActive>().ToggleObject.GetComponent<InputField>().onValueChanged.AddListener(valueChangedEvent);
    }
    private void OnToggle()
    {
        searchButton.GetComponent<ToggleObjectActive>().ToggleButton();
        if (!searchButton.GetComponent<ToggleObjectActive>().State)
        {
            searchInputField.text = "";
            UpdateVisibleItems("");
        }
    }
    void ClearList()
    {
        foreach (GameObject item in allCategoryItemsUI[currentCategoryIndex])
        {
            item.SetActive(false);
        }
    }
    public void UpdateVisibleItems(string currentInputFieldText)
    {
        ClearList();

        if (!gameObject.activeSelf)
        {
            return;
        }

        if (currentInputFieldText == "")
        {
            EnterCategoryOfIndex(currentCategoryIndex);
        }
        else
        {
            foreach (GameObject item in allCategoryItemsUI[currentCategoryIndex])
            {
                if (item.name.ToLower().Contains(currentInputFieldText.ToLower()))
                {
                    item.SetActive(true);
                }
            }
        }
    }
    public void ClearFilter()
    {
        EnterCategoryOfIndex(currentCategoryIndex);
    }
    void SetupCategoryItems(int categoryIndex)
    {
        searchableObjects.Add(new List<GameObject>());
        allCategoryItemsUI.Add(new List<GameObject>());

        categoriesButtons[categoryIndex].GetComponent<Button>().onClick.AddListener(delegate { EnterCategoryOfIndex(categoryIndex); });

        string categoryName = categoriesNames[categoryIndex];
        foreach (Transform child in categories[currentCategoryIndex].transform)
        {
            searchableObjects[categoryIndex].Add(child.gameObject);
            CreateListItemFromObject(child.gameObject, categoryIndex);
            categoryObjects[categoryName].Add(child.gameObject);
            // child.gameObject.SetActive(false);
        }

        categoriesButtons[currentCategoryIndex].GetComponent<ToggleButtonImage>().SetState(false);
    }

    void CreateListItemFromObject(GameObject _object, int categoryIndex)
    {
        GameObject newButton = Instantiate(templateButton);
        newButton.transform.SetParent(templateButton.transform.parent);
        newButton.name = _object.name;
        newButton.transform.GetComponentInChildren<Text>().text = _object.name;
        newButton.transform.localScale = Vector3.one;
        newButton.GetComponent<Button>().onClick.AddListener(delegate { MouseModeManager.Instance.PlaceNewObject(categories[currentCategoryIndex].name.Split(' ')[1], _object); });
        newButton.SetActive(false);
        allCategoryItemsUI[categoryIndex].Add(newButton);
    }

    public void EnterCategoryOfIndex(int index)
    {
        for (int i = 0; i < categories.Count; i++)
        {
            categoriesButtons[i].GetComponent<ToggleButtonImage>().SetState(i == index);
            allCategoryItemsUI[i].ForEach(b => b.SetActive(i == index));
        }
        currentCategoryIndex = index;
    }
}
