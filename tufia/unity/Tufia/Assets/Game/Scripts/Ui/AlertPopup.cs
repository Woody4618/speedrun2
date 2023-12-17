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
public class AlertPopup: BasePopup
{

    public TextMeshProUGUI ResultText;

    private void Awake()
    {
      ServiceFactory.RegisterSingleton(this);
      Root.gameObject.SetActive(false);
      CloseButton.onClick.RemoveAllListeners();
      CloseButton.onClick.AddListener(OnCloseButtonClicked);
    }


    public override void Open(UiService.UiData uiData)
    {
      Root.gameObject.SetActive(true);

    }

}
