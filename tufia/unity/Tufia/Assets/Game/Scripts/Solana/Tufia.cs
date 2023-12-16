using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Solana.Unity;
using Solana.Unity.Programs.Abstract;
using Solana.Unity.Programs.Utilities;
using Solana.Unity.Rpc;
using Solana.Unity.Rpc.Builders;
using Solana.Unity.Rpc.Core.Http;
using Solana.Unity.Rpc.Core.Sockets;
using Solana.Unity.Rpc.Types;
using Solana.Unity.Wallet;
using Tufia;
using Tufia.Program;
using Tufia.Errors;
using Tufia.Accounts;
using Tufia.Types;

namespace Tufia
{
    namespace Accounts
    {
        public partial class GameData
        {
            public static ulong ACCOUNT_DISCRIMINATOR => 13758009850765924589UL;
            public static ReadOnlySpan<byte> ACCOUNT_DISCRIMINATOR_BYTES => new byte[]{237, 88, 58, 243, 16, 69, 238, 190};
            public static string ACCOUNT_DISCRIMINATOR_B58 => "ghYLwVtPH73";
            public ulong IdCounter { get; set; }

            public ulong ActionIndex { get; set; }

            public TileData[][] Data { get; set; }

            public ulong TotalWoodCollected { get; set; }

            public GameAction[] GameActions { get; set; }

            public uint FloorId { get; set; }

            public PublicKey Owner { get; set; }

            public static GameData Deserialize(ReadOnlySpan<byte> _data)
            {
                int offset = 0;
                ulong accountHashValue = _data.GetU64(offset);
                offset += 8;
                if (accountHashValue != ACCOUNT_DISCRIMINATOR)
                {
                    return null;
                }

                GameData result = new GameData();
                result.IdCounter = _data.GetU64(offset);
                offset += 8;
                result.ActionIndex = _data.GetU64(offset);
                offset += 8;
                result.Data = new TileData[10][];
                for (uint resultDataIdx = 0; resultDataIdx < 10; resultDataIdx++)
                {
                    result.Data[resultDataIdx] = new TileData[10];
                    for (uint resultDataresultDataIdxIdx = 0; resultDataresultDataIdxIdx < 10; resultDataresultDataIdxIdx++)
                    {
                        offset += TileData.Deserialize(_data, offset, out var resultDataresultDataIdxresultDataresultDataIdxIdx);
                        result.Data[resultDataIdx][resultDataresultDataIdxIdx] = resultDataresultDataIdxresultDataresultDataIdxIdx;
                    }
                }

                result.TotalWoodCollected = _data.GetU64(offset);
                offset += 8;
                result.GameActions = new GameAction[20];
                for (uint resultGameActionsIdx = 0; resultGameActionsIdx < 20; resultGameActionsIdx++)
                {
                    offset += GameAction.Deserialize(_data, offset, out var resultGameActionsresultGameActionsIdx);
                    result.GameActions[resultGameActionsIdx] = resultGameActionsresultGameActionsIdx;
                }

                result.FloorId = _data.GetU32(offset);
                offset += 4;
                result.Owner = _data.GetPubKey(offset);
                offset += 32;
                return result;
            }
        }

        public partial class PlayerData
        {
            public static ulong ACCOUNT_DISCRIMINATOR => 9264901878634267077UL;
            public static ReadOnlySpan<byte> ACCOUNT_DISCRIMINATOR_BYTES => new byte[]{197, 65, 216, 202, 43, 139, 147, 128};
            public static string ACCOUNT_DISCRIMINATOR_B58 => "ZzeEvyxXcpF";
            public PublicKey Authority { get; set; }

            public string Name { get; set; }

            public uint Level { get; set; }

            public uint Xp { get; set; }

            public uint Health { get; set; }

            public uint MaxHealth { get; set; }

            public uint Damage { get; set; }

            public uint Defence { get; set; }

            public uint Swords { get; set; }

            public uint Shields { get; set; }

            public uint Energy { get; set; }

            public long LastLogin { get; set; }

            public ushort LastId { get; set; }

            public ushort CurrentFloor { get; set; }

            public TileData2 TileData { get; set; }

            public static PlayerData Deserialize(ReadOnlySpan<byte> _data)
            {
                int offset = 0;
                ulong accountHashValue = _data.GetU64(offset);
                offset += 8;
                if (accountHashValue != ACCOUNT_DISCRIMINATOR)
                {
                    return null;
                }

                PlayerData result = new PlayerData();
                result.Authority = _data.GetPubKey(offset);
                offset += 32;
                offset += _data.GetBorshString(offset, out var resultName);
                result.Name = resultName;
                result.Level = _data.GetU32(offset);
                offset += 4;
                result.Xp = _data.GetU32(offset);
                offset += 4;
                result.Health = _data.GetU32(offset);
                offset += 4;
                result.MaxHealth = _data.GetU32(offset);
                offset += 4;
                result.Damage = _data.GetU32(offset);
                offset += 4;
                result.Defence = _data.GetU32(offset);
                offset += 4;
                result.Swords = _data.GetU32(offset);
                offset += 4;
                result.Shields = _data.GetU32(offset);
                offset += 4;
                result.Energy = _data.GetU32(offset);
                offset += 4;
                result.LastLogin = _data.GetS64(offset);
                offset += 8;
                result.LastId = _data.GetU16(offset);
                offset += 2;
                result.CurrentFloor = _data.GetU16(offset);
                offset += 2;
                offset += TileData2.Deserialize(_data, offset, out var resultTileData);
                result.TileData = resultTileData;
                return result;
            }
        }
    }

    namespace Errors
    {
        public enum TufiaErrorKind : uint
        {
            NotEnoughEnergy = 6000U,
            WrongAuthority = 6001U,
            PlayerNotOnBoard = 6002U,
            OutOfBounds = 6003U,
            PlayerAlreadyExists = 6004U,
            BoardIsFull = 6005U,
            PlayerIsAlreadyOnThisTile = 6006U
        }
    }

    namespace Types
    {
        public partial class GameAction
        {
            public ulong ActionId { get; set; }

            public byte ActionType { get; set; }

            public byte FromX { get; set; }

            public byte FromY { get; set; }

            public byte ToX { get; set; }

            public byte ToY { get; set; }

            public TileData Tile { get; set; }

            public ulong Amount { get; set; }

            public int Serialize(byte[] _data, int initialOffset)
            {
                int offset = initialOffset;
                _data.WriteU64(ActionId, offset);
                offset += 8;
                _data.WriteU8(ActionType, offset);
                offset += 1;
                _data.WriteU8(FromX, offset);
                offset += 1;
                _data.WriteU8(FromY, offset);
                offset += 1;
                _data.WriteU8(ToX, offset);
                offset += 1;
                _data.WriteU8(ToY, offset);
                offset += 1;
                offset += Tile.Serialize(_data, offset);
                _data.WriteU64(Amount, offset);
                offset += 8;
                return offset - initialOffset;
            }

            public static int Deserialize(ReadOnlySpan<byte> _data, int initialOffset, out GameAction result)
            {
                int offset = initialOffset;
                result = new GameAction();
                result.ActionId = _data.GetU64(offset);
                offset += 8;
                result.ActionType = _data.GetU8(offset);
                offset += 1;
                result.FromX = _data.GetU8(offset);
                offset += 1;
                result.FromY = _data.GetU8(offset);
                offset += 1;
                result.ToX = _data.GetU8(offset);
                offset += 1;
                result.ToY = _data.GetU8(offset);
                offset += 1;
                offset += TileData.Deserialize(_data, offset, out var resultTile);
                result.Tile = resultTile;
                result.Amount = _data.GetU64(offset);
                offset += 8;
                return offset - initialOffset;
            }
        }

        public partial class TileData
        {
            public byte TileType { get; set; }

            public uint TileLevel { get; set; }

            public PublicKey TileOwner { get; set; }

            public uint TileXp { get; set; }

            public uint TileDamage { get; set; }

            public uint TileDefence { get; set; }

            public uint TileArmor { get; set; }

            public uint TileMaxArmor { get; set; }

            public uint TileHealth { get; set; }

            public uint TileMaxHealth { get; set; }

            public int Serialize(byte[] _data, int initialOffset)
            {
                int offset = initialOffset;
                _data.WriteU8(TileType, offset);
                offset += 1;
                _data.WriteU32(TileLevel, offset);
                offset += 4;
                _data.WritePubKey(TileOwner, offset);
                offset += 32;
                _data.WriteU32(TileXp, offset);
                offset += 4;
                _data.WriteU32(TileDamage, offset);
                offset += 4;
                _data.WriteU32(TileDefence, offset);
                offset += 4;
                _data.WriteU32(TileArmor, offset);
                offset += 4;
                _data.WriteU32(TileMaxArmor, offset);
                offset += 4;
                _data.WriteU32(TileHealth, offset);
                offset += 4;
                _data.WriteU32(TileMaxHealth, offset);
                offset += 4;
                return offset - initialOffset;
            }

            public static int Deserialize(ReadOnlySpan<byte> _data, int initialOffset, out TileData result)
            {
                int offset = initialOffset;
                result = new TileData();
                result.TileType = _data.GetU8(offset);
                offset += 1;
                result.TileLevel = _data.GetU32(offset);
                offset += 4;
                result.TileOwner = _data.GetPubKey(offset);
                offset += 32;
                result.TileXp = _data.GetU32(offset);
                offset += 4;
                result.TileDamage = _data.GetU32(offset);
                offset += 4;
                result.TileDefence = _data.GetU32(offset);
                offset += 4;
                result.TileArmor = _data.GetU32(offset);
                offset += 4;
                result.TileMaxArmor = _data.GetU32(offset);
                offset += 4;
                result.TileHealth = _data.GetU32(offset);
                offset += 4;
                result.TileMaxHealth = _data.GetU32(offset);
                offset += 4;
                return offset - initialOffset;
            }
        }

        public partial class TileData2
        {
            public byte TileType { get; set; }

            public uint TileLevel { get; set; }

            public PublicKey TileOwner { get; set; }

            public uint TileXp { get; set; }

            public uint TileDamage { get; set; }

            public uint TileDefence { get; set; }

            public uint TileArmor { get; set; }

            public uint TileMaxArmor { get; set; }

            public uint TileHealth { get; set; }

            public uint TileMaxHealth { get; set; }

            public int Serialize(byte[] _data, int initialOffset)
            {
                int offset = initialOffset;
                _data.WriteU8(TileType, offset);
                offset += 1;
                _data.WriteU32(TileLevel, offset);
                offset += 4;
                _data.WritePubKey(TileOwner, offset);
                offset += 32;
                _data.WriteU32(TileXp, offset);
                offset += 4;
                _data.WriteU32(TileDamage, offset);
                offset += 4;
                _data.WriteU32(TileDefence, offset);
                offset += 4;
                _data.WriteU32(TileArmor, offset);
                offset += 4;
                _data.WriteU32(TileMaxArmor, offset);
                offset += 4;
                _data.WriteU32(TileHealth, offset);
                offset += 4;
                _data.WriteU32(TileMaxHealth, offset);
                offset += 4;
                return offset - initialOffset;
            }

            public static int Deserialize(ReadOnlySpan<byte> _data, int initialOffset, out TileData2 result)
            {
                int offset = initialOffset;
                result = new TileData2();
                result.TileType = _data.GetU8(offset);
                offset += 1;
                result.TileLevel = _data.GetU32(offset);
                offset += 4;
                result.TileOwner = _data.GetPubKey(offset);
                offset += 32;
                result.TileXp = _data.GetU32(offset);
                offset += 4;
                result.TileDamage = _data.GetU32(offset);
                offset += 4;
                result.TileDefence = _data.GetU32(offset);
                offset += 4;
                result.TileArmor = _data.GetU32(offset);
                offset += 4;
                result.TileMaxArmor = _data.GetU32(offset);
                offset += 4;
                result.TileHealth = _data.GetU32(offset);
                offset += 4;
                result.TileMaxHealth = _data.GetU32(offset);
                offset += 4;
                return offset - initialOffset;
            }
        }
    }

    public partial class TufiaClient : TransactionalBaseClient<TufiaErrorKind>
    {
        public TufiaClient(IRpcClient rpcClient, IStreamingRpcClient streamingRpcClient, PublicKey programId) : base(rpcClient, streamingRpcClient, programId)
        {
        }

        public async Task<Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<GameData>>> GetGameDatasAsync(string programAddress, Commitment commitment = Commitment.Finalized)
        {
            var list = new List<Solana.Unity.Rpc.Models.MemCmp>{new Solana.Unity.Rpc.Models.MemCmp{Bytes = GameData.ACCOUNT_DISCRIMINATOR_B58, Offset = 0}};
            var res = await RpcClient.GetProgramAccountsAsync(programAddress, commitment, memCmpList: list);
            if (!res.WasSuccessful || !(res.Result?.Count > 0))
                return new Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<GameData>>(res);
            List<GameData> resultingAccounts = new List<GameData>(res.Result.Count);
            resultingAccounts.AddRange(res.Result.Select(result => GameData.Deserialize(Convert.FromBase64String(result.Account.Data[0]))));
            return new Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<GameData>>(res, resultingAccounts);
        }

        public async Task<Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<PlayerData>>> GetPlayerDatasAsync(string programAddress, Commitment commitment = Commitment.Finalized)
        {
            var list = new List<Solana.Unity.Rpc.Models.MemCmp>{new Solana.Unity.Rpc.Models.MemCmp{Bytes = PlayerData.ACCOUNT_DISCRIMINATOR_B58, Offset = 0}};
            var res = await RpcClient.GetProgramAccountsAsync(programAddress, commitment, memCmpList: list);
            if (!res.WasSuccessful || !(res.Result?.Count > 0))
                return new Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<PlayerData>>(res);
            List<PlayerData> resultingAccounts = new List<PlayerData>(res.Result.Count);
            resultingAccounts.AddRange(res.Result.Select(result => PlayerData.Deserialize(Convert.FromBase64String(result.Account.Data[0]))));
            return new Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<PlayerData>>(res, resultingAccounts);
        }

        public async Task<Solana.Unity.Programs.Models.AccountResultWrapper<GameData>> GetGameDataAsync(string accountAddress, Commitment commitment = Commitment.Finalized)
        {
            var res = await RpcClient.GetAccountInfoAsync(accountAddress, commitment);
            if (!res.WasSuccessful)
                return new Solana.Unity.Programs.Models.AccountResultWrapper<GameData>(res);
            var resultingAccount = GameData.Deserialize(Convert.FromBase64String(res.Result.Value.Data[0]));
            return new Solana.Unity.Programs.Models.AccountResultWrapper<GameData>(res, resultingAccount);
        }

        public async Task<Solana.Unity.Programs.Models.AccountResultWrapper<PlayerData>> GetPlayerDataAsync(string accountAddress, Commitment commitment = Commitment.Finalized)
        {
            var res = await RpcClient.GetAccountInfoAsync(accountAddress, commitment);
            if (!res.WasSuccessful)
                return new Solana.Unity.Programs.Models.AccountResultWrapper<PlayerData>(res);
            var resultingAccount = PlayerData.Deserialize(Convert.FromBase64String(res.Result.Value.Data[0]));
            return new Solana.Unity.Programs.Models.AccountResultWrapper<PlayerData>(res, resultingAccount);
        }

        public async Task<SubscriptionState> SubscribeGameDataAsync(string accountAddress, Action<SubscriptionState, Solana.Unity.Rpc.Messages.ResponseValue<Solana.Unity.Rpc.Models.AccountInfo>, GameData> callback, Commitment commitment = Commitment.Finalized)
        {
            SubscriptionState res = await StreamingRpcClient.SubscribeAccountInfoAsync(accountAddress, (s, e) =>
            {
                GameData parsingResult = null;
                if (e.Value?.Data?.Count > 0)
                    parsingResult = GameData.Deserialize(Convert.FromBase64String(e.Value.Data[0]));
                callback(s, e, parsingResult);
            }, commitment);
            return res;
        }

        public async Task<SubscriptionState> SubscribePlayerDataAsync(string accountAddress, Action<SubscriptionState, Solana.Unity.Rpc.Messages.ResponseValue<Solana.Unity.Rpc.Models.AccountInfo>, PlayerData> callback, Commitment commitment = Commitment.Finalized)
        {
            SubscriptionState res = await StreamingRpcClient.SubscribeAccountInfoAsync(accountAddress, (s, e) =>
            {
                PlayerData parsingResult = null;
                if (e.Value?.Data?.Count > 0)
                    parsingResult = PlayerData.Deserialize(Convert.FromBase64String(e.Value.Data[0]));
                callback(s, e, parsingResult);
            }, commitment);
            return res;
        }

        public async Task<RequestResult<string>> SendInitPlayerAsync(InitPlayerAccounts accounts, string levelSeed, PublicKey feePayer, Func<byte[], PublicKey, byte[]> signingCallback, PublicKey programId)
        {
            Solana.Unity.Rpc.Models.TransactionInstruction instr = Program.TufiaProgram.InitPlayer(accounts, levelSeed, programId);
            return await SignAndSendTransaction(instr, feePayer, signingCallback);
        }

        public async Task<RequestResult<string>> SendMoveToTileAsync(MoveToTileAccounts accounts, string levelSeed, ushort counter, ulong x, ulong y, PublicKey feePayer, Func<byte[], PublicKey, byte[]> signingCallback, PublicKey programId)
        {
            Solana.Unity.Rpc.Models.TransactionInstruction instr = Program.TufiaProgram.MoveToTile(accounts, levelSeed, counter, x, y, programId);
            return await SignAndSendTransaction(instr, feePayer, signingCallback);
        }

        public async Task<RequestResult<string>> SendMoveToNextFloorAsync(MoveToNextFloorAccounts accounts, string levelSeed, ushort counter, PublicKey feePayer, Func<byte[], PublicKey, byte[]> signingCallback, PublicKey programId)
        {
            Solana.Unity.Rpc.Models.TransactionInstruction instr = Program.TufiaProgram.MoveToNextFloor(accounts, levelSeed, counter, programId);
            return await SignAndSendTransaction(instr, feePayer, signingCallback);
        }

        public async Task<RequestResult<string>> SendBuyNextFloorAsync(BuyNextFloorAccounts accounts, string levelSeed, ushort counter, PublicKey feePayer, Func<byte[], PublicKey, byte[]> signingCallback, PublicKey programId)
        {
            Solana.Unity.Rpc.Models.TransactionInstruction instr = Program.TufiaProgram.BuyNextFloor(accounts, levelSeed, counter, programId);
            return await SignAndSendTransaction(instr, feePayer, signingCallback);
        }

        public async Task<RequestResult<string>> SendResetFloorAsync(ResetFloorAccounts accounts, string levelSeed, ushort counter, PublicKey feePayer, Func<byte[], PublicKey, byte[]> signingCallback, PublicKey programId)
        {
            Solana.Unity.Rpc.Models.TransactionInstruction instr = Program.TufiaProgram.ResetFloor(accounts, levelSeed, counter, programId);
            return await SignAndSendTransaction(instr, feePayer, signingCallback);
        }

        protected override Dictionary<uint, ProgramError<TufiaErrorKind>> BuildErrorsDictionary()
        {
            return new Dictionary<uint, ProgramError<TufiaErrorKind>>{{6000U, new ProgramError<TufiaErrorKind>(TufiaErrorKind.NotEnoughEnergy, "Not enough energy")}, {6001U, new ProgramError<TufiaErrorKind>(TufiaErrorKind.WrongAuthority, "Wrong Authority")}, {6002U, new ProgramError<TufiaErrorKind>(TufiaErrorKind.PlayerNotOnBoard, "Player not on board")}, {6003U, new ProgramError<TufiaErrorKind>(TufiaErrorKind.OutOfBounds, "Out of bounds")}, {6004U, new ProgramError<TufiaErrorKind>(TufiaErrorKind.PlayerAlreadyExists, "PlayerAlreadyExists")}, {6005U, new ProgramError<TufiaErrorKind>(TufiaErrorKind.BoardIsFull, "BoardIsFull")}, {6006U, new ProgramError<TufiaErrorKind>(TufiaErrorKind.PlayerIsAlreadyOnThisTile, "PlayerIsAlreadyOnThisTile")}, };
        }
    }

    namespace Program
    {
        public class InitPlayerAccounts
        {
            public PublicKey Player { get; set; }

            public PublicKey GameData { get; set; }

            public PublicKey Signer { get; set; }

            public PublicKey SystemProgram { get; set; }
        }

        public class MoveToTileAccounts
        {
            public PublicKey SessionToken { get; set; }

            public PublicKey Player { get; set; }

            public PublicKey GameData { get; set; }

            public PublicKey Signer { get; set; }

            public PublicKey SystemProgram { get; set; }
        }

        public class MoveToNextFloorAccounts
        {
            public PublicKey SessionToken { get; set; }

            public PublicKey Player { get; set; }

            public PublicKey GameData { get; set; }

            public PublicKey Signer { get; set; }

            public PublicKey SystemProgram { get; set; }
        }

        public class BuyNextFloorAccounts
        {
            public PublicKey SessionToken { get; set; }

            public PublicKey Player { get; set; }

            public PublicKey GameData { get; set; }

            public PublicKey Signer { get; set; }

            public PublicKey SystemProgram { get; set; }
        }

        public class ResetFloorAccounts
        {
            public PublicKey SessionToken { get; set; }

            public PublicKey Player { get; set; }

            public PublicKey GameData { get; set; }

            public PublicKey Signer { get; set; }

            public PublicKey SystemProgram { get; set; }
        }

        public static class TufiaProgram
        {
            public static Solana.Unity.Rpc.Models.TransactionInstruction InitPlayer(InitPlayerAccounts accounts, string levelSeed, PublicKey programId)
            {
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Player, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.GameData, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Signer, true), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SystemProgram, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(4819994211046333298UL, offset);
                offset += 8;
                offset += _data.WriteBorshString(levelSeed, offset);
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction MoveToTile(MoveToTileAccounts accounts, string levelSeed, ushort counter, ulong x, ulong y, PublicKey programId)
            {
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SessionToken == null ? programId : accounts.SessionToken, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Player, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.GameData, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Signer, true), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SystemProgram, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(5279588053843574468UL, offset);
                offset += 8;
                offset += _data.WriteBorshString(levelSeed, offset);
                _data.WriteU16(counter, offset);
                offset += 2;
                _data.WriteU64(x, offset);
                offset += 8;
                _data.WriteU64(y, offset);
                offset += 8;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction MoveToNextFloor(MoveToNextFloorAccounts accounts, string levelSeed, ushort counter, PublicKey programId)
            {
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SessionToken == null ? programId : accounts.SessionToken, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Player, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.GameData, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Signer, true), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SystemProgram, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(2481710203484716370UL, offset);
                offset += 8;
                offset += _data.WriteBorshString(levelSeed, offset);
                _data.WriteU16(counter, offset);
                offset += 2;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction BuyNextFloor(BuyNextFloorAccounts accounts, string levelSeed, ushort counter, PublicKey programId)
            {
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SessionToken == null ? programId : accounts.SessionToken, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Player, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.GameData, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Signer, true), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SystemProgram, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(7033119409976356332UL, offset);
                offset += 8;
                offset += _data.WriteBorshString(levelSeed, offset);
                _data.WriteU16(counter, offset);
                offset += 2;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction ResetFloor(ResetFloorAccounts accounts, string levelSeed, ushort counter, PublicKey programId)
            {
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SessionToken == null ? programId : accounts.SessionToken, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Player, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.GameData, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Signer, true), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SystemProgram, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(7303134863959624869UL, offset);
                offset += 8;
                offset += _data.WriteBorshString(levelSeed, offset);
                _data.WriteU16(counter, offset);
                offset += 2;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }
        }
    }
}
