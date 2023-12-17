using TMPro;
using Tufia.Types;
using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    public Image GreenBar;
    public TextMeshProUGUI HealthText;
    public TextMeshProUGUI DefenceText;
    public TextMeshProUGUI DamageText;
    public TextMeshProUGUI LevelText;
    public Image ArmorBar;
    public GameObject Root;

    private void Awake()
    {
      Root.gameObject.SetActive(false);
    }

    public void SetData(TileData2 tileData)
    {
      Root.gameObject.SetActive(tileData.TileType == AnchorService.BUILDING_TYPE_PLAYER ||
                                tileData.TileType == AnchorService.BUILDING_TYPE_ENEMY );
      if (tileData.TileMaxHealth > 0)
      {
        GreenBar.transform.localScale = new Vector3(tileData.TileHealth / (float) tileData.TileMaxHealth, 1, 1);
      }
      else
      {
        GreenBar.transform.localScale = new Vector3(0, 1, 1);
      }
      ArmorBar.gameObject.SetActive(tileData.TileMaxArmor>0);
      if (tileData.TileMaxArmor > 0)
      {
        ArmorBar.transform.localScale = new Vector3(tileData.TileArmor / (float) tileData.TileMaxArmor, 1, 1);
      }
      HealthText.text = $"{tileData.TileHealth}/{tileData.TileMaxHealth}";
      DefenceText.text = $"{tileData.TileDefence}";
      DamageText.text = $"{tileData.TileDamage}";
      LevelText.text = $"{tileData.TileLevel}";
    }

    public void SetData(TileData tileData)
    {
      Root.gameObject.SetActive(tileData.TileType == AnchorService.BUILDING_TYPE_PLAYER ||
                                tileData.TileType == AnchorService.BUILDING_TYPE_ENEMY );
      if (tileData.TileMaxHealth > 0)
      {
        GreenBar.transform.localScale = new Vector3(tileData.TileHealth / (float) tileData.TileMaxHealth, 1, 1);
      }
      else
      {
        GreenBar.transform.localScale = new Vector3(0, 1, 1);
      }
      ArmorBar.gameObject.SetActive(tileData.TileMaxArmor>0);
      if (tileData.TileMaxArmor > 0)
      {
        ArmorBar.transform.localScale = new Vector3(tileData.TileArmor / (float) tileData.TileMaxArmor, 1, 1);
      }
      HealthText.text = $"{tileData.TileHealth}/{tileData.TileMaxHealth}";
      DefenceText.text = $"{tileData.TileDefence}";
      DamageText.text = $"{tileData.TileDamage}";
      LevelText.text = $"{tileData.TileLevel}";
    }
}
