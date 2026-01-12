using System;
using System.Collections.Generic;
using ConsoleProject.GameObjects;
using ConsoleProject.Utils;

namespace ConsoleProject.Scenes
{
    public class TownScene : Scene
    {
        private Tile[,] _field = new Tile[11, 80];
        private PlayerCharacter _player;

        private const int ViewWidth = 25;
        private const int ViewHeight = 11;
        private const int CenterX = 12;

        private const int LeftEndX = 0;
        private const int RightEndX = 79;

        private const int StartLineX = 12;
        private const int SpawnX = 16;
        private const int SpawnY = 5;

        private const int PropTopY = 2;
        private const int PropBotY = 8;
        private const int WalkMinY = 3;
        private const int WalkMaxY = 7;

        private const int EyeBreedDelayMoves = 15;
        private const int EyeSpawnEveryMoves = 6;
        private const int EyeMaxCount = 10;

        private const int GhostStartDelayMoves = 3;
        private const int GhostMoveEveryMoves = 2;
        private bool _ghostWallActive = false;
        private int _ghostWallX = RightEndX;

        private const int DollStartDelayMoves = 8;
        private const int DollMoveEveryMoves = 4;
        private bool _dollActive = false;
        private Vector _dollPos;

        private int _movesSinceStageStart = 0;
        private Vector _lastPlayerPos;

        private readonly ViewportMapRenderer _renderer = new ViewportMapRenderer(ViewWidth, ViewHeight);
        private readonly Random _rng = new Random();

        private int _stage = 1;

        private bool _hasAnomaly = false;

        private string _message = "";

        private readonly List<Vector> _baseProps = new List<Vector>();

        private struct OverlayCell { public string Token; public bool Deadly; }
        private readonly Dictionary<(int x, int y), OverlayCell> _overlay = new Dictionary<(int x, int y), OverlayCell>();
        private int _eyeCount = 0;

        private enum AnomalyType
        {
            None,
            ReplaceProp,
            SwapTwoProps,
            RemoveOneProp,
            DuplicateOneProp,
            GhostWallBloodTrail,
            EyeReplaceAndBreed,
            DollChase,
            BlockDrop
        }

        private AnomalyType _anomalyType = AnomalyType.None;

        public TownScene(PlayerCharacter player) => Init(player);

        public void Init(PlayerCharacter player)
        {
            _player = player;

            for (int y = 0; y < _field.GetLength(0); y++)
                for (int x = 0; x < _field.GetLength(1); x++)
                    _field[y, x] = new Tile(new Vector(x, y));
        }

        public override void Enter()
        {
            _player.Field = _field;

            PlacePlayer(new Vector(SpawnX, SpawnY));
            _lastPlayerPos = _player.Position;

            StartStage(1);
        }

        public override void Update()
        {
            _player.Update();

            bool moved = (_player.Position.X != _lastPlayerPos.X || _player.Position.Y != _lastPlayerPos.Y);
            if (moved)
            {
                _movesSinceStageStart++;
                OnPlayerMovedTurn();
                _lastPlayerPos = _player.Position;
            }

            if (IsTouchingDeadly())
            {
                ResetToStage1("💀 이상현상에 닿았습니다!");
                return;
            }

            if (_player.Position.X <= LeftEndX) Judge(isLeft: true);
            else if (_player.Position.X >= RightEndX) Judge(isLeft: false);
        }

        public override void Render()
        {
            int mapWidth = _field.GetLength(1);

            int startX = _player.Position.X - CenterX;
            if (startX < 0) startX = 0;

            int maxStartX = mapWidth - ViewWidth;
            if (startX > maxStartX) startX = maxStartX;

            _renderer.Render(
                _field,
                startX,
                _player.Position,
                _stage,
                _message,
                GetOverlayToken
            );
        }

        public override void Exit()
        {
            _field[_player.Position.Y, _player.Position.X].OnTileObject = null;
            _player.Field = null;
        }

        private void StartStage(int stage)
        {
            _stage = stage;
            _message = "";
            _renderer.ForceRedraw();

            _overlay.Clear();
            _eyeCount = 0;

            _ghostWallActive = false;
            _ghostWallX = RightEndX;

            _dollActive = false;

            _movesSinceStageStart = 0;
            _lastPlayerPos = _player.Position;

            BuildBaseLayout();

            if (_stage == 1)
            {
                _hasAnomaly = false;
                _anomalyType = AnomalyType.None;
                return;
            }

            RollAnomalyOne();
            ApplyAnomalyOne();
        }

        private void BuildBaseLayout()
        {
            _baseProps.Clear();

            for (int y = 0; y < _field.GetLength(0); y++)
                for (int x = 0; x < _field.GetLength(1); x++)
                {
                    if (_field[y, x].OnTileObject != null && _field[y, x].OnTileObject != _player)
                        _field[y, x].OnTileObject = null;
                }

            _field[PropTopY, StartLineX].OnTileObject = new StaticProp(EmojiTiles.StartFlag, new Vector(StartLineX, PropTopY), "Landmark");
            _field[PropBotY, StartLineX].OnTileObject = new StaticProp(EmojiTiles.StartFlag, new Vector(StartLineX, PropBotY), "Landmark");

            PlaceBaseProp(24, PropTopY, EmojiTiles.Board);
            PlaceBaseProp(34, PropTopY, EmojiTiles.Clock);
            PlaceBaseProp(44, PropTopY, EmojiTiles.Locker);
            PlaceBaseProp(54, PropTopY, EmojiTiles.Chair);
            PlaceBaseProp(64, PropTopY, EmojiTiles.Books);

            PlaceBaseProp(26, PropBotY, EmojiTiles.Bin);
            PlaceBaseProp(36, PropBotY, EmojiTiles.Sign);
            PlaceBaseProp(46, PropBotY, EmojiTiles.Light);
            PlaceBaseProp(56, PropBotY, EmojiTiles.Poster);
            PlaceBaseProp(66, PropBotY, EmojiTiles.Exting);
        }

        private void PlaceBaseProp(int x, int y, string token)
        {
            var pos = new Vector(x, y);
            _field[y, x].OnTileObject = new StaticProp(token, pos, "Base");
            _baseProps.Add(pos);
        }

        private void RollAnomalyOne()
        {
            _hasAnomaly = _rng.NextDouble() < 0.85;

            if (!_hasAnomaly)
            {
                _anomalyType = AnomalyType.None;
                return;
            }

            _anomalyType = PickWeightedAnomaly();
        }

        private AnomalyType PickWeightedAnomaly()
        {
            var weighted = new (AnomalyType type, int w)[]
            {
                (AnomalyType.ReplaceProp,          22),
                (AnomalyType.BlockDrop,            18),
                (AnomalyType.DollChase,            14),
                (AnomalyType.EyeReplaceAndBreed,   12),
                (AnomalyType.SwapTwoProps,         10),
                (AnomalyType.DuplicateOneProp,     10),
                (AnomalyType.RemoveOneProp,         8),
                (AnomalyType.GhostWallBloodTrail,   6),
            };

            if (_stage >= 6)
            {
                if (_rng.NextDouble() < 0.25)
                    return AnomalyType.GhostWallBloodTrail;
            }

            int total = 0;
            for (int i = 0; i < weighted.Length; i++) total += weighted[i].w;

            int roll = _rng.Next(0, total);
            for (int i = 0; i < weighted.Length; i++)
            {
                roll -= weighted[i].w;
                if (roll < 0) return weighted[i].type;
            }

            return weighted[0].type;
        }

        private void ApplyAnomalyOne()
        {
            switch (_anomalyType)
            {
                case AnomalyType.None:
                    return;

                case AnomalyType.GhostWallBloodTrail:
                    _ghostWallActive = true;
                    _ghostWallX = RightEndX;
                    _message = "…복도 끝에서 끌리는 소리가 난다.";
                    return;

                case AnomalyType.EyeReplaceAndBreed:
                    ApplyEyeReplace();
                    return;

                case AnomalyType.DollChase:
                    StartDollChase();
                    _message = "…앞에서 발소리가 들린다.";
                    return;

                case AnomalyType.BlockDrop:
                    StartBlockDrop();
                    _message = "쿵! 무언가 떨어졌다.";
                    return;

                case AnomalyType.ReplaceProp:
                    ApplyPropReplace();
                    return;

                case AnomalyType.SwapTwoProps:
                    ApplySwapTwoProps();
                    return;

                case AnomalyType.RemoveOneProp:
                    ApplyRemoveOneProp();
                    return;

                case AnomalyType.DuplicateOneProp:
                    ApplyDuplicateOneProp();
                    return;
            }
        }

        private void ApplyPropReplace()
        {
            string[] scary = { EmojiTiles.Skull, EmojiTiles.Spider, EmojiTiles.Blood, EmojiTiles.Eye, EmojiTiles.Doll };

            Vector p = _baseProps[_rng.Next(_baseProps.Count)];
            if (_field[p.Y, p.X].OnTileObject is StaticProp sp)
                sp.EmojiToken = scary[_rng.Next(scary.Length)];
        }

        private void ApplySwapTwoProps()
        {
            Vector a = _baseProps[_rng.Next(_baseProps.Count)];
            Vector b = _baseProps[_rng.Next(_baseProps.Count)];
            if (a.X == b.X && a.Y == b.Y) return;

            var pa = _field[a.Y, a.X].OnTileObject as StaticProp;
            var pb = _field[b.Y, b.X].OnTileObject as StaticProp;
            if (pa == null || pb == null) return;

            string tmp = pa.EmojiToken;
            pa.EmojiToken = pb.EmojiToken;
            pb.EmojiToken = tmp;
        }

        private void ApplyRemoveOneProp()
        {
            Vector p = _baseProps[_rng.Next(_baseProps.Count)];
            _field[p.Y, p.X].OnTileObject = null;
        }

        private void ApplyDuplicateOneProp()
        {
            Vector src = _baseProps[_rng.Next(_baseProps.Count)];
            var sp = _field[src.Y, src.X].OnTileObject as StaticProp;
            if (sp == null) return;

            (int x, int y)[] slots =
            {
                (18, PropTopY), (70, PropTopY),
                (18, PropBotY), (70, PropBotY)
            };

            foreach (var s in slots)
            {
                if (_field[s.y, s.x].OnTileObject == null)
                {
                    _field[s.y, s.x].OnTileObject = new StaticProp(sp.EmojiToken, new Vector(s.x, s.y), "Anomaly");
                    break;
                }
            }
        }

        private void ApplyEyeReplace()
        {
            Vector p = _baseProps[_rng.Next(_baseProps.Count)];
            if (_field[p.Y, p.X].OnTileObject is StaticProp sp)
                sp.EmojiToken = EmojiTiles.Eye;
        }

        private void UpdateEyeBreeding()
        {
            if (_anomalyType != AnomalyType.EyeReplaceAndBreed) return;
            if (_movesSinceStageStart < EyeBreedDelayMoves) return;
            if (_eyeCount >= EyeMaxCount) return;

            int after = _movesSinceStageStart - EyeBreedDelayMoves;
            if (after % EyeSpawnEveryMoves != 0) return;

            for (int attempt = 0; attempt < 20; attempt++)
            {
                int xMin = Math.Max(_player.Position.X + 2, StartLineX + 1);
                int xMax = RightEndX - 1;
                if (xMin > xMax) xMin = StartLineX + 1;

                int x = _rng.Next(xMin, xMax + 1);
                int y = _rng.Next(WalkMinY, WalkMaxY + 1);

                if (x == _player.Position.X && y == _player.Position.Y) continue;
                if (_overlay.ContainsKey((x, y))) continue;

                _overlay[(x, y)] = new OverlayCell { Token = EmojiTiles.Eye, Deadly = true };
                _eyeCount++;
                break;
            }
        }

        private void UpdateGhostWall()
        {
            if (_anomalyType != AnomalyType.GhostWallBloodTrail) return;
            if (!_ghostWallActive) return;

            if (_movesSinceStageStart < GhostStartDelayMoves) return;

            int after = _movesSinceStageStart - GhostStartDelayMoves;
            if (after % GhostMoveEveryMoves != 0) return;

            int oldX = _ghostWallX;
            _ghostWallX = Math.Max(LeftEndX, _ghostWallX - 1);

            for (int y = WalkMinY; y <= WalkMaxY; y++)
                _overlay[(oldX, y)] = new OverlayCell { Token = EmojiTiles.Blood, Deadly = true };
        }

        private void StartDollChase()
        {
            _dollActive = true;
            _dollPos = new Vector(RightEndX - 2, _rng.Next(WalkMinY, WalkMaxY + 1));
        }

        private void UpdateDollChase()
        {
            if (_anomalyType != AnomalyType.DollChase) return;
            if (!_dollActive) return;
            if (_movesSinceStageStart < DollStartDelayMoves) return;

            int after = _movesSinceStageStart - DollStartDelayMoves;
            if (after % DollMoveEveryMoves != 0) return;

            int dx = Math.Sign(_player.Position.X - _dollPos.X);
            int dy = Math.Sign(_player.Position.Y - _dollPos.Y);

            Vector next = _dollPos;
            if (dx != 0) next = new Vector(_dollPos.X + dx, _dollPos.Y);
            else if (dy != 0) next = new Vector(_dollPos.X, _dollPos.Y + dy);

            if (next.Y < WalkMinY) next = new Vector(next.X, WalkMinY);
            if (next.Y > WalkMaxY) next = new Vector(next.X, WalkMaxY);

            _dollPos = next;
        }

        private void StartBlockDrop()
        {
            for (int attempt = 0; attempt < 30; attempt++)
            {
                int x = _rng.Next(StartLineX + 8, RightEndX - 5);
                int y = _rng.Next(WalkMinY, WalkMaxY + 1);

                if (x == _player.Position.X && y == _player.Position.Y) continue;
                if (_field[y, x].OnTileObject != null) continue;

                _field[y, x].OnTileObject = new StaticProp(EmojiTiles.Block, new Vector(x, y), "Anomaly");
                break;
            }
        }

        private void OnPlayerMovedTurn()
        {
            UpdateGhostWall();
            UpdateEyeBreeding();
            UpdateDollChase();
        }

        private bool IsTouchingDeadly()
        {
            int px = _player.Position.X;
            int py = _player.Position.Y;

            if (_ghostWallActive &&
                _anomalyType == AnomalyType.GhostWallBloodTrail &&
                px == _ghostWallX && py >= WalkMinY && py <= WalkMaxY)
                return true;

            if (_dollActive && _anomalyType == AnomalyType.DollChase &&
                px == _dollPos.X && py == _dollPos.Y)
                return true;

            if (_overlay.TryGetValue((px, py), out var cell) && cell.Deadly)
                return true;

            return false;
        }

        private string GetOverlayToken(int worldX, int worldY)
        {
            if (worldX == StartLineX && worldY >= WalkMinY && worldY <= WalkMaxY)
                return EmojiTiles.StartLine;

            if (_ghostWallActive &&
                _anomalyType == AnomalyType.GhostWallBloodTrail &&
                worldX == _ghostWallX && worldY >= WalkMinY && worldY <= WalkMaxY)
                return EmojiTiles.Ghost;

            if (_dollActive && _anomalyType == AnomalyType.DollChase &&
                worldX == _dollPos.X && worldY == _dollPos.Y)
                return EmojiTiles.Doll;

            if (_overlay.TryGetValue((worldX, worldY), out var cell))
                return cell.Token;

            return "";
        }

        private void Judge(bool isLeft)
        {
            bool correct =
                (!_hasAnomaly && !isLeft) ||
                (_hasAnomaly && isLeft);

            if (correct)
            {
                if (_stage >= 8)
                {
                    _message = "🎉 폐교에서 탈출했다… (Stage 8 클리어)";
                    Render();
                    Console.ReadKey(true);
                    Environment.Exit(0);
                    return;
                }

                _message = "✅ 정답!";
                _stage++;
                PlacePlayer(new Vector(SpawnX, SpawnY));
                StartStage(_stage);
            }
            else
            {
                ResetToStage1("❌ 오답!");
            }
        }

        private void ResetToStage1(string reason)
        {
            _message = $"{reason} Stage 1로 돌아갑니다.";
            _stage = 1;

            PlacePlayer(new Vector(SpawnX, SpawnY));
            StartStage(_stage);
        }

        private void PlacePlayer(Vector pos)
        {
            if (_player.Position.X >= 0 && _player.Position.X < 80 &&
                _player.Position.Y >= 0 && _player.Position.Y < 11)
                _field[_player.Position.Y, _player.Position.X].OnTileObject = null;

            _player.Position = pos;
            _field[pos.Y, pos.X].OnTileObject = _player;
        }
    }
}
