using System.Collections;
using DG.Tweening;
using Frictionless;
using Game.Scripts.Ui;
using Services;
using TMPro;
using Tufia.Types;
using UnityEngine;

/// <summary>
/// Screen that shows the battle
/// </summary>
public class ItemPopup: BasePopup
{
    public HealthBar PlayerHealthBar;

    public GameObject PlayerGO;

    public GameObject[] Items;

    public TextMeshProUGUI ResultText;

    private void Awake()
    {
      ServiceFactory.RegisterSingleton(this);
      Root.gameObject.SetActive(false);
      CloseButton.onClick.RemoveAllListeners();
      CloseButton.onClick.AddListener(OnCloseButtonClicked);
    }

    public void ShowItem(bool isBlueChest, TileData playerTileData)
    {
      PlayerHealthBar.SetData(playerTileData);
      PlayerHealthBar.Root.gameObject.SetActive(true);

      Root.gameObject.SetActive(true);
      ResultText.text = isBlueChest ? "Golden Chest (Keep forever)" : "Normal Chest (Loose when you die)";
      foreach (var item in Items)
      {
        item.gameObject.SetActive(false);
      }
      Items[Random.Range(0,Items.Length)].gameObject.SetActive(true);
    }

    public override void Open(UiService.UiData uiData)
    {

    }

}
