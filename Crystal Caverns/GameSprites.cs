using System.Drawing;

namespace Crystal_Caverns.Utils
{
    public static class GameSprites
    {
        
        public static Image GetPlayerSprite()
        {
            return Properties.Resources.PlayerSprite;
        }

        public static Image GetPlatformSprite()
        {
            return Properties.Resources.PlatformSprite;
        }

        public static Image GetCrystalSprite()
        {
            return Properties.Resources.CrystalSprite;
        }

        public static Image GetEnemySprite()
        {
            return Properties.Resources.EnemySprite;
        }

        public static Image GetDoorClosedSprite()
        {
            return Properties.Resources.DoorClosedSprite;
        }
        public static Image GetDoorOpenSprite()
        {
            return Properties.Resources.DoorOpenSprite;
        }
    }
}