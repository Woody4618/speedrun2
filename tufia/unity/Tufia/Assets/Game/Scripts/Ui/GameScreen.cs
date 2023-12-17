using System;
using System.Collections;
using DefaultNamespace;
using DG.Tweening;
using Frictionless;
using Tufia.Accounts;
using Solana.Unity.SDK;
using Services;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// This is the screen which handles the interaction with the anchor program.
/// It checks if there is a game account already and has a button to call a function in the program.
/// </summary>
public class GameScreen : MonoBehaviour
{
    public Button ChuckWoodSessionButton;
    public Button ResetFloorButton;
    public Button BuyFloorButton;
    public Button NftsButton;
    public Button InitGameDataButton;

    public TextMeshProUGUI EnergyAmountText;
    public TextMeshProUGUI CurrentFloorText;
    public TextMeshProUGUI NextEnergyInText;
    public TextMeshProUGUI TotalLogAvailableText;
    public TextMeshProUGUI FloorOwnerText;

    public GameObject HasNoPlayerOnFloorRoot;
    public GameObject FloorIsEmptyRoot;
    public GameObject NotInitializedRoot;
    public GameObject InitializedRoot;
    public GameObject ActionFx;
    public GameObject ActionFxPosition;
    public GameObject Tree;

    private Vector3 CharacterStartPosition;
    private PlayerData currentPlayerData;
    private GameData currentGameData;

    void Start()
    {
        ChuckWoodSessionButton.onClick.AddListener(OnMovePlayerButtonClicked);
        NftsButton.onClick.AddListener(OnNftsButtonClicked);
        ResetFloorButton.onClick.AddListener(OnResetFloorButtonClicked);
        BuyFloorButton.onClick.AddListener(OnBuyFloorButtonClicked);
        InitGameDataButton.onClick.AddListener(OnInitGameDataButtonClicked);
        CharacterStartPosition = ChuckWoodSessionButton.transform.localPosition;
        // In case we are not logged in yet load the LoginScene
        if (Web3.Account == null)
        {
            SceneManager.LoadScene("LoginScene");
            return;
        }
        StartCoroutine(UpdateNextEnergy());

        AnchorService.OnPlayerDataChanged += OnPlayerDataChanged;
        AnchorService.OnGameDataChanged += OnGameDataChanged;
        AnchorService.OnInitialDataLoaded += UpdateContent;
    }

    private void OnDestroy()
    {
        AnchorService.OnPlayerDataChanged -= OnPlayerDataChanged;
        AnchorService.OnGameDataChanged -= OnGameDataChanged;
        AnchorService.OnInitialDataLoaded -= UpdateContent;
    }

    private void OnEnable()
    {
        StartCoroutine(UpdateNextEnergy());
    }

    private async void OnInitGameDataButtonClicked()
    {
        // On local host we probably dont have the session key progeam, but can just sign with the in game wallet instead.
        await AnchorService.Instance.InitAccounts(!Web3.Rpc.NodeAddress.AbsoluteUri.Contains("localhost"));
    }

    private void OnNftsButtonClicked()
    {
        ServiceFactory.Resolve<UiService>().OpenPopup(UiService.ScreenType.NftListPopup, new NftListPopupUiData(false, Web3.Wallet));
    }

    private IEnumerator UpdateNextEnergy()
    {
        while (true)
        {
            yield return new WaitForSeconds(1);
            UpdateContent();
        }
    }

    private void OnPlayerDataChanged(PlayerData playerData)
    {
        if (currentPlayerData != null && currentPlayerData.CurrentFloor < playerData.CurrentFloor)
        {
            // TODO: Move player to new position
            //ChuckWoodSessionButton.transform.DOLocalMove(CharacterStartPosition, 0.2f);
        }

        currentPlayerData = playerData;
        UpdateContent();
    }

    private void OnGameDataChanged(GameData gameData, bool reset)
    {
      if (gameData == null)
        {
          currentGameData = gameData;
          return;
        }

      FloorOwnerText.text = "Owner: " + gameData.Owner;
        if (currentGameData != null && currentGameData.TotalWoodCollected != gameData.TotalWoodCollected)
        {
            Tree.transform.DOKill();
            Tree.transform.localScale = Vector3.one;
            Tree.transform.DOPunchScale(Vector3.one * 0.1f, 0.1f);
            Instantiate(ActionFx, ActionFxPosition.transform.position, Quaternion.identity);
        }

        var totalLogAvailable = AnchorService.MAX_WOOD_PER_TREE - gameData.TotalWoodCollected;
        TotalLogAvailableText.text = totalLogAvailable + " Wood available.";
        currentGameData = gameData;
    }

    private void UpdateContent()
    {
        var isInitialized = AnchorService.Instance.IsInitialized();
        NotInitializedRoot.SetActive(!isInitialized);
        InitGameDataButton.gameObject.SetActive(!isInitialized && AnchorService.Instance.CurrentPlayerData == null);
        InitializedRoot.SetActive(isInitialized);

        if (AnchorService.Instance.CurrentPlayerData == null)
        {
            return;
        }

        var playerCell = ServiceFactory.Resolve<BoardManager>().GetCellByOwner(AnchorService.Instance.CurrentPlayerData.Authority);
        HasNoPlayerOnFloorRoot.gameObject.SetActive(playerCell == null && AnchorService.Instance.CurrentGameData != null);
        FloorIsEmptyRoot.gameObject.SetActive(AnchorService.Instance.CurrentGameData == null);

        var lastLoginTime = AnchorService.Instance.CurrentPlayerData.LastLogin;
        var timePassed = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - lastLoginTime;

        while (
            timePassed >= AnchorService.TIME_TO_REFILL_ENERGY &&
            AnchorService.Instance.CurrentPlayerData.Energy < AnchorService.MAX_ENERGY
        ) {
            AnchorService.Instance.CurrentPlayerData.Energy += 1;
            AnchorService.Instance.CurrentPlayerData.LastLogin += AnchorService.TIME_TO_REFILL_ENERGY;
            timePassed -= AnchorService.TIME_TO_REFILL_ENERGY;
        }

        var timeUntilNextRefill = AnchorService.TIME_TO_REFILL_ENERGY - timePassed;

        if (timeUntilNextRefill > 0)
        {
            NextEnergyInText.text = timeUntilNextRefill.ToString();
        }
        else
        {
            NextEnergyInText.text = "";
        }

        EnergyAmountText.text = AnchorService.Instance.CurrentPlayerData.Energy.ToString();
        CurrentFloorText.text = AnchorService.Instance.CurrentPlayerData.CurrentFloor.ToString();
    }

    private void OnMovePlayerButtonClicked()
    {
        ChuckWoodSessionButton.transform.localPosition = CharacterStartPosition;
        ChuckWoodSessionButton.transform.DOLocalMove(CharacterStartPosition + Vector3.up * 10, 0.3f);
        AnchorService.Instance.MoveToTile(!Web3.Rpc.NodeAddress.AbsoluteUri.Contains("localhost"), () =>
        {
            // Do something with the result. The websocket update in onPlayerDataChanged will come a bit earlier
        }, 1, 1);
    }

    private void OnResetFloorButtonClicked()
    {
        AnchorService.Instance.ResetFloor();
    }

    private void OnBuyFloorButtonClicked()
    {
        AnchorService.Instance.BuyNewFloor(() =>
        {

        });
    }
}
