using System;
using System.Collections.Generic;
using ConsoleProject.GameObjects;
using ConsoleProject.Utils;

namespace ConsoleProject.Scenes
{
    public class TownScene : Scene
    {

        // 맵 구현 완료 크기 조정할거면 수치 조절
        private readonly Tile[,] _field = new Tile[11, 80];
        private PlayerCharacter _player = null!;

        // 화면(카메라) -> 유니티 에서는 안쓸듯?
        // 타일 수가 보이는 수치로 카메라 처럼 구현 *왠만하면 손대지 말것*
        // CenterX : 플레이어를 화면 가운데쯤에 두기 위한 보정값
        private const int ViewWidth = 25;
        private const int ViewHeight = 11;
        private const int CenterX = 12;

        // 맵 양 끝(판정 지점)
        private const int LeftEndX = 0;
        private const int RightEndX = 79;

        // 기준이 되는 시작선(항상 고정) *손 대지 말것*
        // X,Y = 스테이지 시작 위치
        private const int StartLineX = 12;
        private const int SpawnX = 16;
        private const int SpawnY = 5;

        // PropTopY/PropBotY : 벽 바로 앞 라인(기준 오브젝트가 놓이는 줄)
        // WalkMinY/WalkMaxY : 플레이어가 다니는 통로 범위

        private const int PropTopY = 2;
        private const int PropBotY = 8;
        private const int WalkMinY = 3;
        private const int WalkMaxY = 7;

        // EyeSpawnEveryMoves : 번식 주기
        // EyeMaxCount : 눈 오버레이 최대 개수
        private const int EyeBreedDelayMoves = 15;
        private const int EyeSpawnEveryMoves = 5;
        private const int EyeMaxCount = 15;

        // GhostMoveEveryMoves : 이동 주기
        private const int GhostStartDelayMoves = 3;
        private const int GhostMoveEveryMoves = 2;
        // _ghostWallX : 유령벽의 현재 X 위치
        private bool _ghostWallActive;
        private int _ghostWallX = RightEndX;

        // 곰인형 이벤트 내용은 위와 동일
        private const int DollStartDelayMoves = 8;
        private const int DollMoveEveryMoves = 4;

        private bool _dollActive;
        private Vector _dollPos;

        // _movesSinceStageStart : "실제로 이동한 경우"만 증가하는 카운트
        // _lastPlayerPos : 이동했는지 판정하기 위한 이전 위치
        private int _movesSinceStageStart;
        private Vector _lastPlayerPos;

        private readonly ViewportMapRenderer _renderer = new ViewportMapRenderer(ViewWidth, ViewHeight);
        private readonly Random _rng = new Random();

        private int _stage = 1;
        private bool _hasAnomaly;
        private string _message = "";

        // _baseProps : 기본 배치된 오브젝트 좌표 목록
        private readonly List<Vector> _baseProps = new List<Vector>();

        // Deadly=true인 경우 플레이어가 밟으면 즉사 판정이 난다.
        private struct OverlayCell
        {
            public string Token;
            public bool Deadly;
        }

        private readonly Dictionary<(int x, int y), OverlayCell> _overlay = new Dictionary<(int x, int y), OverlayCell>();
        private int _eyeCount;

        // -----------------------------
        // 스테이지당 1개 이상현상 타입
        // -----------------------------
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

        public TownScene(PlayerCharacter player)
        {
            Init(player);
        }

        private void Init(PlayerCharacter player)
        {
            _player = player;

            for (int y = 0; y < _field.GetLength(0); y++)
            {
                for (int x = 0; x < _field.GetLength(1); x++)
                {
                    _field[y, x] = new Tile(new Vector(x, y));
                }
            }
        }

        public override void Enter()
        {
            _player.Field = _field;
            PlacePlayer(new Vector(SpawnX, SpawnY));

            _lastPlayerPos = _player.Position;
            StartStage(1, clearMessage: true);
        }

        public override void Update()
        {
            _player.Update();

            // 콘솔은 입력 대기 때문에 "실시간 업데이트"가 아님 
            // 실제 이동했는지 기준으로 턴을 증가시킨다.
            bool moved = _player.Position.X != _lastPlayerPos.X || _player.Position.Y != _lastPlayerPos.Y;
            if (moved)
            {
                _movesSinceStageStart++;
                OnPlayerMovedTurn();
                _lastPlayerPos = _player.Position;
            }

            if (TryGetDeathMessage(out var deathMsg))
            {
                ResetToStage1(deathMsg);
                return;
            }

            // 양 끝 도달 시 정답/오답 판정
            if (_player.Position.X <= LeftEndX) Judge(true);
            else if (_player.Position.X >= RightEndX) Judge(false);
        }

        public override void Render()
        {
            int mapWidth = _field.GetLength(1);

            // 플레이어를 기준으로 화면 시작 X를 계산해서 "카메라"처럼 보이게 한다.
            // 재성 강사님 코드가 궁금하다.
            int startX = _player.Position.X - CenterX;
            if (startX < 0) startX = 0;

            int maxStartX = mapWidth - ViewWidth;
            if (startX > maxStartX) startX = maxStartX;

            _renderer.Render(_field, startX, _player.Position, _stage, _message, GetOverlayToken);
        }

        public override void Exit()
        {
            _field[_player.Position.Y, _player.Position.X].OnTileObject = null;
            _player.Field = null;
        }

        private void StartStage(int stage, bool clearMessage)
        {
            _stage = stage;
            if (clearMessage) _message = "";

            _renderer.ForceRedraw();

            _overlay.Clear();
            _eyeCount = 0;

            _ghostWallActive = false;
            _ghostWallX = RightEndX;

            _dollActive = false;

            _movesSinceStageStart = 0;
            _lastPlayerPos = _player.Position;

            BuildBaseLayout();

            // Stage 1은 반드시 정상(이상 없음)
            if (_stage == 1)
            {
                _hasAnomaly = false;
                _anomalyType = AnomalyType.None;
                return;
            }

            RollAnomalyOne();
            ApplyAnomalyOne();
        }


        // 플레이어 외 오브젝트를 정리하고,시작선/기준 오브젝트 10개를 동일한 위치에 다시 배치한다.
        private void BuildBaseLayout()
        {
            _baseProps.Clear();

            for (int y = 0; y < _field.GetLength(0); y++)
            {
                for (int x = 0; x < _field.GetLength(1); x++)
                {
                    if (_field[y, x].OnTileObject != null && _field[y, x].OnTileObject != _player)
                        _field[y, x].OnTileObject = null;
                }
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

        // _hasAnomaly : 이번 스테이지에 이상이 있는지 여부
        // _anomalyType: 실제로 어떤 이상이 적용될지
        private void RollAnomalyOne()
        {
            _hasAnomaly = _rng.NextDouble() < 0.75;

            if (!_hasAnomaly)
            {
                _anomalyType = AnomalyType.None;
                return;
            }

            _anomalyType = PickWeightedAnomaly();
        }

        // 가중치 랜덤: 자주 나오게 하고 싶은 이벤트는 weight를 키운다.
        private AnomalyType PickWeightedAnomaly()
        {
            var weighted = new (AnomalyType type, int w)[]
            {
                (AnomalyType.ReplaceProp,         22),
                (AnomalyType.BlockDrop,           18),
                (AnomalyType.DollChase,           14),
                (AnomalyType.EyeReplaceAndBreed,  12),
                (AnomalyType.SwapTwoProps,        10),
                (AnomalyType.DuplicateOneProp,    10),
                (AnomalyType.RemoveOneProp,        8),
                (AnomalyType.GhostWallBloodTrail,  6),
            };

            // 주석을 안달면 뭔 내용인지 눈에 안들어온다.
            // 스테이지가 점점 커질수록
            if (_stage >= 6 && _rng.NextDouble() < 0.25)
                return AnomalyType.GhostWallBloodTrail;      // 유령을 더 자주 출몰하게 한다.


            int total = 0;
            for (int i = 0; i < weighted.Length; i++) total += weighted[i].w;

            int roll = _rng.Next(total);
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
                    return;

                case AnomalyType.EyeReplaceAndBreed:
                    ApplyEyeReplace();
                    return;

                case AnomalyType.DollChase:
                    StartDollChase();
                    return;

                case AnomalyType.BlockDrop:
                    StartBlockDrop();
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
            string[] scaryEmojis =
            {
                EmojiTiles.Skull, EmojiTiles.Spider, EmojiTiles.Blood, EmojiTiles.Eye, EmojiTiles.Doll
            };

            Vector chosenPos = _baseProps[_rng.Next(_baseProps.Count)];

            if (_field[chosenPos.Y, chosenPos.X].OnTileObject is StaticProp prop)
                prop.EmojiToken = scaryEmojis[_rng.Next(scaryEmojis.Length)];
        }

        private void ApplySwapTwoProps()
        {
            Vector firstPos = _baseProps[_rng.Next(_baseProps.Count)];
            Vector secondPos = _baseProps[_rng.Next(_baseProps.Count)];

            if (firstPos.X == secondPos.X && firstPos.Y == secondPos.Y) return;

            var firstProp = _field[firstPos.Y, firstPos.X].OnTileObject as StaticProp;
            var secondProp = _field[secondPos.Y, secondPos.X].OnTileObject as StaticProp;
            if (firstProp == null || secondProp == null) return;

            string tempToken = firstProp.EmojiToken;
            firstProp.EmojiToken = secondProp.EmojiToken;
            secondProp.EmojiToken = tempToken;
        }

        private void ApplyRemoveOneProp()
        {
            Vector chosenPos = _baseProps[_rng.Next(_baseProps.Count)];
            _field[chosenPos.Y, chosenPos.X].OnTileObject = null;
        }

        private void ApplyDuplicateOneProp()
        {
            Vector sourcePos = _baseProps[_rng.Next(_baseProps.Count)];
            var sourceProp = _field[sourcePos.Y, sourcePos.X].OnTileObject as StaticProp;
            if (sourceProp == null) return;

            (int x, int y)[] spawnSlots =
            {
                (18, PropTopY), (70, PropTopY),
                (18, PropBotY), (70, PropBotY)
            };

            for (int i = 0; i < spawnSlots.Length; i++)
            {
                var slot = spawnSlots[i];
                if (_field[slot.y, slot.x].OnTileObject != null) continue;

                _field[slot.y, slot.x].OnTileObject = new StaticProp(sourceProp.EmojiToken, new Vector(slot.x, slot.y), "Anomaly");
                break;
            }
        }


        private void ApplyEyeReplace()
        {
            Vector chosenPropPos = _baseProps[_rng.Next(_baseProps.Count)];

            // 해당 칸에 StaticProp이 있을 때만 "눈"으로 교체
            if (_field[chosenPropPos.Y, chosenPropPos.X].OnTileObject is StaticProp prop)
            {
                prop.EmojiToken = EmojiTiles.Eye;
            }
        }

        private void UpdateEyeBreeding()
        {
            // 이 이상현상이 아닐 땐 아무것도 하지 않음
            if (_anomalyType != AnomalyType.EyeReplaceAndBreed) return;

            // 번식 시작 전 대기 턴
            if (_movesSinceStageStart < EyeBreedDelayMoves) return;

            // 최대 개수 제한
            if (_eyeCount >= EyeMaxCount) return;

            // Delay 이후 경과 턴을 기준으로 주기 체크
            int movesAfterDelay = _movesSinceStageStart - EyeBreedDelayMoves;
            if (movesAfterDelay % EyeSpawnEveryMoves != 0) return;

            // 너무 멀리/불가능한 위치를 뽑지 않도록 여러 번 시도
            for (int attempt = 0; attempt < 20; attempt++)
            {
                // 플레이어보다 최소 2칸 앞에서만 생성되게(시야/압박 연출용)
                int minSpawnX = Math.Max(_player.Position.X + 2, StartLineX + 1);
                int maxSpawnX = RightEndX - 1;

                // 플레이어가 오른쪽 끝에 가까우면 범위를 안전하게 보정
                if (minSpawnX > maxSpawnX)
                    minSpawnX = StartLineX + 1;

                int spawnX = _rng.Next(minSpawnX, maxSpawnX + 1);
                int spawnY = _rng.Next(WalkMinY, WalkMaxY + 1);

                // 플레이어 위치면 스킵
                if (spawnX == _player.Position.X && spawnY == _player.Position.Y) continue;

                // 이미 overlay가 있으면 스킵(겹침 방지)
                if (_overlay.ContainsKey((spawnX, spawnY))) continue;

                // 치명 눈 셀 생성
                _overlay[(spawnX, spawnY)] = new OverlayCell
                {
                    Token = EmojiTiles.Eye,
                    Deadly = true
                };

                _eyeCount++;
                break;
            }
        }

        // ------------------------------------------------------------
        // Anomaly: GhostWall + BloodTrail
        // - 일정 턴 이후부터 유령 벽이 일정 주기로 왼쪽으로 이동
        // - 지나간 자리에 피(치명 overlay)를 깔아 흔적을 남김
        // ------------------------------------------------------------
        private void UpdateGhostWall()
        {
            if (_anomalyType != AnomalyType.GhostWallBloodTrail) return;
            if (!_ghostWallActive) return;

            // 시작 전 대기 턴
            if (_movesSinceStageStart < GhostStartDelayMoves) return;

            int movesAfterDelay = _movesSinceStageStart - GhostStartDelayMoves;
            if (movesAfterDelay % GhostMoveEveryMoves != 0) return;

            int previousWallX = _ghostWallX;

            // 왼쪽으로 1칸 이동하되, 맵 밖으로 나가지 않게 제한
            _ghostWallX = Math.Max(LeftEndX, _ghostWallX - 1);

            // 유령 벽이 지나간 칸(세로 라인)에 피 흔적을 남김
            for (int y = WalkMinY; y <= WalkMaxY; y++)
            {
                _overlay[(previousWallX, y)] = new OverlayCell
                {
                    Token = EmojiTiles.Blood,
                    Deadly = true
                };
            }
        }

        // ------------------------------------------------------------
        // Anomaly: DollChase
        // - 시작 시 인형을 오른쪽에서 스폰
        // - 일정 턴 이후부터 플레이어를 추격(맨해튼 거리 기반으로 가까울수록 더 자주 움직임)
        // ------------------------------------------------------------
        private void StartDollChase()
        {
            _dollActive = true;

            int spawnY = _rng.Next(WalkMinY, WalkMaxY + 1);
            _dollPos = new Vector(RightEndX - 2, spawnY);
        }

        private void UpdateDollChase()
        {
            if (_anomalyType != AnomalyType.DollChase) return;
            if (!_dollActive) return;
            if (_movesSinceStageStart < DollStartDelayMoves) return;

            int movesAfterDelay = _movesSinceStageStart - DollStartDelayMoves;

            int distanceX = Math.Abs(_player.Position.X - _dollPos.X);
            int distanceY = Math.Abs(_player.Position.Y - _dollPos.Y);
            int manhattanDistance = distanceX + distanceY;

            // 기본 이동 주기(가까워질수록 더 자주 이동)
            int moveEvery = DollMoveEveryMoves;
            if (manhattanDistance <= 6) moveEvery = Math.Max(1, DollMoveEveryMoves - 1);
            if (manhattanDistance <= 3) moveEvery = 1;

            if (movesAfterDelay % moveEvery != 0) return;

            // 플레이어 쪽으로 이동 방향 결정
            int stepX = Math.Sign(_player.Position.X - _dollPos.X);
            int stepY = Math.Sign(_player.Position.Y - _dollPos.Y);

            bool moveVerticalFirst = (distanceY > distanceX) || (_rng.NextDouble() < 0.35);

            Vector nextPos = _dollPos;

            if (moveVerticalFirst)
            {
                if (stepY != 0) nextPos = new Vector(_dollPos.X, _dollPos.Y + stepY);
                else if (stepX != 0) nextPos = new Vector(_dollPos.X + stepX, _dollPos.Y);
            }
            else
            {
                if (stepX != 0) nextPos = new Vector(_dollPos.X + stepX, _dollPos.Y);
                else if (stepY != 0) nextPos = new Vector(_dollPos.X, _dollPos.Y + stepY);
            }

            if (nextPos.Y < WalkMinY) nextPos = new Vector(nextPos.X, WalkMinY);
            if (nextPos.Y > WalkMaxY) nextPos = new Vector(nextPos.X, WalkMaxY);

            _dollPos = nextPos;
        }

        private void StartBlockDrop()
        {
            for (int attempt = 0; attempt < 30; attempt++)
            {
                int dropX = _rng.Next(StartLineX + 8, RightEndX - 5);
                int dropY = _rng.Next(WalkMinY, WalkMaxY + 1);

                // 플레이어 위치에 떨어뜨리면 즉사
                if (dropX == _player.Position.X && dropY == _player.Position.Y) continue;

                // 이미 뭔가 있으면 제외
                if (_field[dropY, dropX].OnTileObject != null) continue;

                _field[dropY, dropX].OnTileObject = new StaticProp(EmojiTiles.Block, new Vector(dropX, dropY), "Anomaly");
                break;
            }
        }

        private void OnPlayerMovedTurn()
        {
            UpdateGhostWall();
            UpdateEyeBreeding();
            UpdateDollChase();
        }

        private bool TryGetDeathMessage(out string message)
        {
            int px = _player.Position.X;
            int py = _player.Position.Y;

            if (_ghostWallActive &&
                _anomalyType == AnomalyType.GhostWallBloodTrail &&
                px == _ghostWallX && py >= WalkMinY && py <= WalkMaxY)
            {
                message = "유령벽에 닿았다";
                return true;
            }

            if (_dollActive &&
                _anomalyType == AnomalyType.DollChase &&
                px == _dollPos.X && py == _dollPos.Y)
            {
                message = "인형에게 잡혔다";
                return true;
            }

            if (_overlay.TryGetValue((px, py), out var cell) && cell.Deadly)
            {
                if (cell.Token == EmojiTiles.Blood)
                {
                    message = "피를 밟았다";
                    return true;
                }

                if (cell.Token == EmojiTiles.Eye)
                {
                    message = "눈에 닿았다";
                    return true;
                }

                message = "이상현상에 닿았다";
                return true;
            }

            message = "";
            return false;
        }

        private string GetOverlayToken(int worldX, int worldY)
        {
            bool isOnWalkLine = worldY >= WalkMinY && worldY <= WalkMaxY;

            // 시작선은 항상 표시
            if (worldX == StartLineX && isOnWalkLine)
                return EmojiTiles.StartLine;

            // 유령벽 표시(고스트)
            if (_ghostWallActive &&
                _anomalyType == AnomalyType.GhostWallBloodTrail &&
                worldX == _ghostWallX && isOnWalkLine)
                return EmojiTiles.Ghost;

            // 인형 추격자 표시
            if (_dollActive &&
                _anomalyType == AnomalyType.DollChase &&
                worldX == _dollPos.X && worldY == _dollPos.Y)
                return EmojiTiles.Doll;

            // 그 외 overlay(피, 눈 등)
            if (_overlay.TryGetValue((worldX, worldY), out var overlayCell))
                return overlayCell.Token;

            return "";
        }

        private void Judge(bool choseLeft)
        {
            bool isCorrect =
                (!_hasAnomaly && !choseLeft) ||   // 이상 없음 => 오른쪽이 정답
                (_hasAnomaly && choseLeft);       // 이상 있음 => 왼쪽이 정답

            if (isCorrect)
            {
                // 최종 스테이지 클리어 처리
                if (_stage >= 8)
                {
                    _message = "클리어";
                    Render();
                    Console.ReadKey(true);
                    Environment.Exit(0);
                    return;
                }

                int nextStage = _stage + 1;

                // 다음 스테이지 시작 준비: 플레이어를 스폰 위치로 되돌림
                PlacePlayer(new Vector(SpawnX, SpawnY));
                StartStage(nextStage, clearMessage: true);

                _message = "정답";
            }
            else
            {
                ResetToStage1("오답");
            }
        }

        private void ResetToStage1(string reason)
        {
            PlacePlayer(new Vector(SpawnX, SpawnY));
            StartStage(1, clearMessage: true);
            _message = reason;
        }

        private void PlacePlayer(Vector newPos)
        {
            // 현재 플레이어가 맵 안에 있을 때만 "기존 칸 제거" 실행
            if (_player.Position.X >= 0 && _player.Position.X < _field.GetLength(1) &&
                _player.Position.Y >= 0 && _player.Position.Y < _field.GetLength(0))
            {
                _field[_player.Position.Y, _player.Position.X].OnTileObject = null;
            }

            _player.Position = newPos;
            _field[newPos.Y, newPos.X].OnTileObject = _player;
        }
    }
}
