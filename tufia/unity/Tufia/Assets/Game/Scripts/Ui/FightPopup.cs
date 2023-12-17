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
public class FightPopup: BasePopup
{
    public HealthBar PlayerHealthBar;
    public HealthBar EnemyHealthBar;
    public HealthBar Enemy2HealthBar;

    public GameObject PlayerGO;
    public GameObject PlayerEnemyGO;
    public GameObject EnemyGO;

    public TextMeshProUGUI ResultText;

    private bool isPlayerFight;

    private void Awake()
    {
      ServiceFactory.RegisterSingleton(this);
      Root.gameObject.SetActive(false);
      CloseButton.onClick.RemoveAllListeners();
      CloseButton.onClick.AddListener(OnCloseButtonClicked);
    }

    public void StartBattle(TileData2 player, TileData enemy, bool isPlayerFight)
    {
      this.isPlayerFight = isPlayerFight;
      PlayerGO.GetComponentInChildren<Animator>().speed = 0.5f;
      PlayerGO.gameObject.SetActive(true);

      PlayerEnemyGO.gameObject.SetActive(isPlayerFight);
      EnemyGO.gameObject.SetActive(!isPlayerFight);

      PlayerEnemyGO.GetComponentInChildren<Animator>().speed = 0.5f;
      EnemyGO.GetComponentInChildren<Animator>().speed = 1;

      Root.gameObject.SetActive(true);
      ResultText.gameObject.SetActive(false);
      StartCoroutine(StartBattleRoutine(player, enemy));
    }

    public IEnumerator StartBattleRoutine(TileData2 player, TileData enemy)
    {
      player.TileArmor = player.TileMaxArmor;
      enemy.TileArmor = enemy.TileMaxArmor;
      PlayerHealthBar.SetData(player);
      EnemyHealthBar.SetData(enemy);
      Enemy2HealthBar.SetData(enemy);

      while (player.TileHealth > 0 && enemy.TileHealth > 0)
      {
        uint playerDamage = enemy.TileDamage > player.TileDefence ? enemy.TileDamage - player.TileDefence : 1;
        uint enemyDamage = player.TileDamage > enemy.TileDefence ? player.TileDamage - enemy.TileDefence : 1;

        if (player.TileArmor > 0)
        {
          player.TileArmor = player.TileArmor > playerDamage ? player.TileArmor - playerDamage : 0;
        }
        else
        {
          player.TileHealth = player.TileHealth > playerDamage ? player.TileHealth - playerDamage : 0;
        }

        if (enemy.TileArmor > 0)
        {
          enemy.TileArmor = enemy.TileArmor > enemyDamage ? enemy.TileArmor - enemyDamage : 0;
        }
        else
        {
          enemy.TileHealth = enemy.TileHealth > enemyDamage ? enemy.TileHealth - enemyDamage : 0;
        }

        yield return new WaitForSeconds(0.5f);
        PlayerGO.transform.DOPunchScale(new Vector3(5f, 5f, 5f), 0.2f);
        EnemyGO.transform.DOPunchScale(new Vector3(5f, 5f, 5f), 0.2f);
        PlayerHealthBar.SetData(player);
        EnemyHealthBar.SetData(enemy);
        Enemy2HealthBar.SetData(enemy);
      }

      yield return new WaitForSeconds(0.5f);

      ResultText.gameObject.SetActive(true);
      if (player.TileHealth == 0)
      {
        PlayerGO.gameObject.SetActive(false);
        //EnemyGO.GetComponentInChildren<Animator>().speed = 0;
        EnemyGO.GetComponentInChildren<Animator>().Play("Idle01");
        PlayerEnemyGO.GetComponentInChildren<Animator>().Play("Idle01");

        PlayerGO.GetComponentInChildren<Animator>().Play("Die");
        ResultText.text = "You died ... temporarily!";
        Debug.Log("Player died");
      }
      else
      {
        //EnemyGO.gameObject.SetActive(false);
        //PlayerGO.GetComponentInChildren<Animator>().speed = 0;
        PlayerGO.GetComponentInChildren<Animator>().Play("Idle_Battle_SwordAndShield");
        EnemyGO.GetComponentInChildren<Animator>().Play("Die");
        PlayerEnemyGO.GetComponentInChildren<Animator>().Play("Die");

        ResultText.text = "You won! +2XP";
        Debug.Log("Enemy died");
      }

      Root.gameObject.SetActive(true);
    }

    public override void Open(UiService.UiData uiData)
    {
        /*var nftListPopupUiData = (uiData as NftListPopupUiData);

        if (nftListPopupUiData == null)
        {
            Debug.LogError("Wrong ui data for nft list popup");
            return;
        }

        NftItemListView.UpdateContent();
        NftItemListView.SetData(nft =>
        {
            // when an nft was selected we want to close the popup so we can start the game.
            Close();
        });

        UpdateOwnCollectionStatus();
        base.Open(uiData);*/
    }


}
