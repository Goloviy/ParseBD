using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ViewDropdown : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown dropdown;
    [SerializeField] private List<Item> items;

    private Dictionary<string, Item> itemDictionary;

    public event Action<Item> OnChangeValueDropdown;
    
    private void Start()
    {
        itemDictionary = new Dictionary<string, Item>();
        dropdown.ClearOptions();
        foreach (var item in items)
        {
            itemDictionary.Add(item.itemName, item);
            var option = new TMP_Dropdown.OptionData
            {
                text = item.itemName,
                image = item.icon
            };
            dropdown.options.Add(option);
        }
        dropdown.onValueChanged.AddListener(ChangeValueDropdown);;
    }

    private void ChangeValueDropdown(int _value)
    {
        var item = itemDictionary[items[_value].itemName];
        OnChangeValueDropdown?.Invoke(item);
        Debug.Log(dropdown.options[_value].text);
    }

    public void InitDefaultDropdown()
    {
        dropdown.value = 0;
        OnChangeValueDropdown?.Invoke(itemDictionary[items[0].itemName]);
    }
}
