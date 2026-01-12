namespace ConsoleProject.Utils
{
    public static class EmojiTiles
    {
        // 바닥은 비움(렌더러가 셀을 공백으로 정리)
        public const string Floor = "";

        // 벽 / 플레이어
        public const string Wall = "🧱";
        public const string Player = "🧍";

        // 기본(정상) 오브젝트
        public const string Board = "📌";
        public const string Clock = "🕒";
        public const string Locker = "🧳";
        public const string Chair = "🪑";
        public const string Bin = "🗑️";
        public const string Sign = "🪧";
        public const string Light = "💡";
        public const string Poster = "📄";
        public const string Books = "📚";
        public const string Exting = "🧯";

        // 시작선(고정 랜드마크)
        public const string StartFlag = "🏁";
        public const string StartLine = "🟩";

        // 이상현상 토큰
        public const string Eye = "👁️";
        public const string Ghost = "👻";
        public const string Blood = "🩸";
        public const string Skull = "💀";
        public const string Spider = "🕷️";
        public const string Doll = "🧸";

        // 장애물(막기용, 즉사는 아님)
        public const string Block = "🧳";
    }
}
