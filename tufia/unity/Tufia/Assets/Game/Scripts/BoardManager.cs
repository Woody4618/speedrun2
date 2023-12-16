using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Frictionless;
using Solana.Unity.Wallet;
using SolPlay.Scripts.Ui;
using Tufia.Accounts;
using Tufia.Types;
using Unity.VisualScripting;
using UnityEngine;

namespace DefaultNamespace
{
    public class BoardManager : MonoBehaviour
    {
        public const int WIDTH = 10;
        public const int HEIGHT = 10;

        public const int ACTION_TYPE_MOVE = 0;
        public const int ACTION_TYPE_FIGHT = 1;
        public const int ACTION_TYPE_OPEN_CHEST = 2;
        public const int ACTION_TYPE_RESET = 3;
        public const int ACTION_TYPE_PLAYER_DIED = 4;

        public Tile TilePrefab;
        public Cell CellPrefab;
        public Cell[,] AllCells = new Cell[WIDTH, HEIGHT];
        public List<Tile> tiles = new List<Tile>();
        public TileConfig[] tileConfigs;
        public TextBlimp3D CoinBlimpPrefab;
        public TextBlimp3D FightBlimp;

        public bool IsWaiting;

        private bool isInitialized;
        private Dictionary<ulong, GameAction> alreadyPrerformedGameActions = new Dictionary<ulong, GameAction>();
        private bool HasPlayedInitialAnimations = false;
        private GameData CurrentBaordAccount;

        private void Awake()
        {
            ServiceFactory.RegisterSingleton(this);
        }

        private void Start()
        {
            AnchorService.OnPlayerDataChanged += OnPlayerDataChange;
            AnchorService.OnGameDataChanged += OnBoardDataChange;

            // Crete Cells
            for (int i = 0; i < WIDTH; i++)
            {
                for (int j = 0; j < HEIGHT; j++)
                {
                    Cell cellInstance = Instantiate(CellPrefab, transform);
                    cellInstance.transform.position = new Vector3(1.02f * i, 0, -1.02f * j);
                    cellInstance.Init(i, j, null);
                    AllCells[i,j] = cellInstance;
                }
            }
        }

        private void OnDestroy()
        {
          AnchorService.OnPlayerDataChanged -= OnPlayerDataChange;
          AnchorService.OnGameDataChanged -= OnBoardDataChange;
        }

        private void OnPlayerDataChange(PlayerData obj)
        {
            // Nothing to do here? O.O
        }

        private void OnGameReset()
        {
            isInitialized = false;
            foreach (Tile tile in tiles)
            {
                Destroy(tile.gameObject);
            }
            tiles.Clear();
            //alreadyPrerformedGameActions.Clear();
            for (int i = 0; i < WIDTH; i++)
            {
                for (int j = 0; j < HEIGHT; j++)
                {
                    AllCells[i, j].Tile = null;
                }
            }
        }

        private void OnBoardDataChange(GameData boardAccount, bool reset)
        {
            if (boardAccount == null)
            {
              OnGameReset();
              return;
            }
            CurrentBaordAccount = boardAccount;

            OnGameActionHistoryChange(boardAccount, reset);
            SetData(boardAccount, reset);
        }

        private async void OnGameActionHistoryChange(GameData gameData, bool reset)
        {
            if (reset)
            {
              alreadyPrerformedGameActions.Clear();
              HasPlayedInitialAnimations = false;
            }
            if (!HasPlayedInitialAnimations)
            {
                foreach (GameAction gameAction in gameData.GameActions)
                {
                    if (gameAction.ActionId == 0)
                    {
                        continue;
                    }

                    if (alreadyPrerformedGameActions.ContainsKey(gameAction.ActionId))
                    {
                      alreadyPrerformedGameActions[gameAction.ActionId] = gameAction;
                    }
                    else
                    {
                      alreadyPrerformedGameActions.Add(gameAction.ActionId, gameAction);
                    }
                }

                HasPlayedInitialAnimations = true;
                return;
            }

            foreach (GameAction gameAction in gameData.GameActions)
            {
                if (!alreadyPrerformedGameActions.ContainsKey(gameAction.ActionId))
                {
                    var targetCell = GetCell(gameAction.ToX, gameAction.ToY);

                    Debug.Log($"Target Cell: {targetCell.X} {targetCell.Y}");
                    if (gameAction.ActionType == ACTION_TYPE_MOVE)
                    {
                        var fromCell = GetCell(gameAction.FromX, gameAction.FromY);
                        Debug.Log($"From Cell: {fromCell.X} {fromCell.Y}");

                        var tileConfig = FindTileConfigByTileData(gameAction.Tile);
                        targetCell.Tile.Init(tileConfig, gameAction.Tile, false);

                        var emptyConfig = FindTileConfigByName("Empty");
                        fromCell.Tile.Init(emptyConfig, new TileData(), false);

                        fromCell.Tile.transform.DOKill();
                        fromCell.Tile.transform.DOMove(targetCell.transform.position, 0.3f).OnComplete(() =>
                        {
                          var tileConfig = FindTileConfigByTileData(gameAction.Tile);
                          targetCell.Tile.Init(tileConfig, gameAction.Tile, true);
                          targetCell.Tile.transform.position = targetCell.transform.position;

                          var emptyConfig = FindTileConfigByName("Empty");
                          fromCell.Tile.Init(emptyConfig, new TileData(), true);
                          fromCell.Tile.transform.position = fromCell.transform.position;
                        });

                        // TODO: Animate and kill the last item
                        //var fromCell = GetCell(gameAction.FromX, gameAction.FromY);
                        //fromCell.Tile = null;
                    }

                    if (gameAction.ActionType == ACTION_TYPE_FIGHT)
                    {
                        // nothin
                    }

                    if (gameAction.ActionType == ACTION_TYPE_OPEN_CHEST)
                    {
                        // nothin
                    }

                    if (gameAction.ActionType == ACTION_TYPE_RESET)
                    {
                        // todo
                    }

                    if (gameAction.ActionType == ACTION_TYPE_FIGHT)
                    {
                        PerformFightAction(gameAction, targetCell);
                    }

                    if (gameAction.ActionType == ACTION_TYPE_PLAYER_DIED)
                    {
                        OnGameReset();
                        await AnchorService.Instance.SubscribeToPlayerDataUpdates();
                        await AnchorService.Instance.UnSubscribeToGameDataUpdates(true);
                        await AnchorService.Instance.SubscribeToGameDataUpdates(true);
                        //CreateStartingTiles(CurrentBaordAccount);
                        isInitialized = false;
                        return;
                    }

                    alreadyPrerformedGameActions.Add(gameAction.ActionId, gameAction);
                }
            }
        }

        private async UniTask PerformFightAction(GameAction gameAction, Cell targetCell)
        {
            var tileConfig = FindTileConfigByTileData(gameAction.Tile);
            var text = "-" + gameAction.Amount;
            targetCell.Tile.Init(tileConfig, gameAction.Tile, true);
            targetCell.Tile.GetComponentInChildren<Animator>().Play("Attack");
            await UniTask.Delay(800);

            // TODO: DO we want an nft as character?
            /*Nft nft = null;
            try
            {
                var rpc = Web3.Wallet.ActiveRpcClient;
                nft = await Nft.TryGetNftData(gameAction.Avatar, rpc).AsUniTask();
            }
            catch (Exception e)
            {
                Debug.LogError("Could not load nft" + e);
            }

            if (nft == null)
            {
                nft = ServiceFactory.Resolve<NftService>().CreateDummyLocalNft(gameAction.Avatar);
            }

            var blimp = Instantiate(FightBlimp);
            blimp.SetData(text, null, tileConfig);
            blimp.transform.position = targetCell.transform.position + new Vector3(0, 1.89f, -1.88f);

            blimp.SetData(text, nft, tileConfig);
            blimp.AddComponent<DestroyDelayed>();*/
        }

        public void SetData(GameData boardAccount, bool reset)
        {
            if (!isInitialized)
            {
                OnGameReset();
                CreateStartingTiles(boardAccount);
                isInitialized = true;
            }
            else
            {
              if (reset)
              {
                OnGameReset();
                CreateStartingTiles(boardAccount);
              }
                //SpawnNewTile(playerData.NewTileX, playerData.NewTileY, playerData.NewTileLevel);
            }

            bool anyTileOutOfSync = false;
            // Compare tiles:
            for (int i = 0; i < WIDTH; i++)
            {
                for (int j = 0; j < HEIGHT; j++)
                {
                    var cell = GetCell(j, i);
                    if (boardAccount.Data[j][i].TileType != 0 && cell.Tile == null)
                    {
                        anyTileOutOfSync = true;
                        Debug.LogWarning("Tiles out of sync.");
                    }else
                    {
                      if (boardAccount.Data[j][i].TileType != cell.Tile.currentConfig.building_type)
                      {
                        anyTileOutOfSync = true;
                        Debug.LogWarning($"Tiles out of sync. x {i} y {j} from socket: {boardAccount.Data[j][i]} board: {GetCell(i, j).Tile.currentConfig.Number} ");
                      }
                    }
                }
            }

            if (anyTileOutOfSync || reset)
            {
                RefreshFromPlayerdata(CurrentBaordAccount);
                return;
            }

            IsWaiting = false;
        }

        public void RefreshFromPlayerdata(GameData baordAccount)
        {
            OnGameReset();
            CreateStartingTiles(baordAccount);
            isInitialized = true;
            IsWaiting = false;
        }

        public Cell GetCell(int x, int y)
        {
          if (x >= 0 && x < WIDTH && y >= 0 && y < HEIGHT) {
            return AllCells[x, y];
          }

          return null;
        }

        public Cell GetCellByOwner(PublicKey owner)
        {
          foreach (var cell in AllCells)
          {
            if (cell.Tile != null && cell.Tile.currentTileData != null &&
                cell.Tile.currentTileData.TileOwner == owner &&
                cell.Tile.currentTileData.TileType == AnchorService.BUILDING_TYPE_PLAYER)
            {
              return cell;
            }
          }

          return null;
        }

        public Cell GetAdjacentCell(Cell cell, Vector2Int direction)
        {
            int adjecentX = cell.X + direction.x;
            int adjecentY = cell.Y - direction.y;

            return GetCell(adjecentX, adjecentY);
        }

        private IEnumerator DestroyAfterSeconds(TextBlimp3D blimp)
        {
            yield return new WaitForSeconds(2);
            Destroy(blimp.gameObject);
        }

        private int IndexOf(TileConfig state)
        {
            for (int i = 0; i < tileConfigs.Length; i++)
            {
                if (state == tileConfigs[i]) {
                    return i;
                }
            }

            return -1;
        }

        private void CreateStartingTiles(GameData playerData)
        {
            for (int x = 0; x < WIDTH; x++)
            {
                for (int y = 0; y < HEIGHT; y++)
                {
                    SpawnNewTile(x, y, playerData.Data[x][y]);
                }
            }
        }

        private void SpawnNewTile(int i, int j, TileData tileData)
        {
            var targetCell = GetCell(i, j);
            if (targetCell.Tile != null)
            {
                // TODO: Refresh only the tiles that changed
                //Debug.LogError("Target cell already full: " + targetCell.Tile.currentConfig.Number);
                //return;
            }

            // TODO: do we need sounds?
            /*if (SoundToggle.IsSoundEnabled())
            {
                SpawnAudioSource.PlayOneShot(SpawnClip);
            }*/

            Tile tileInstance = Instantiate(TilePrefab, transform);

            tileInstance.transform.position = targetCell.transform.position;
            TileConfig newConfig = FindTileConfigByTileData(tileData);

            tileInstance.Init(newConfig, tileData);
            tileInstance.Spawn(targetCell);
            tiles.Add(tileInstance);
        }

        public TileConfig FindTileConfigByTileData(TileData tileData)
        {
            foreach (var tileConfig in tileConfigs)
            {
                if (tileConfig.building_type == tileData.TileType)
                {
                    return tileConfig;
                }
            }

            return tileConfigs[tileConfigs.Length - 1];
        }

        public TileConfig FindTileConfigByName(string name)
        {
            foreach (var tileConfig in tileConfigs)
            {
                if (tileConfig.BuildingName == name)
                {
                    return tileConfig;
                }
            }

            return tileConfigs[tileConfigs.Length - 1];
        }
    }
}
