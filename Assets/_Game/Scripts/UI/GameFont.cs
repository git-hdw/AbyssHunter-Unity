using UnityEngine;

namespace AbyssHunter.UI
{
    public static class GameFont
    {
        private static Font chinese;

        public static Font Chinese
        {
            get
            {
                if (chinese == null)
                    chinese = Resources.Load<Font>("Fonts/NotoSansCJKsc-Regular");
                return chinese;
            }
        }
    }
}
