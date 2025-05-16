using System.Drawing;
using System.Windows.Forms;
using Crystal_Caverns.Utils;

namespace Crystal_Caverns.Model
{
    public class Collectible : GameObject
    {
        public int Value { get; protected set; }
        public bool IsCollected { get; protected set; }
        
        public Collectible(float x, float y, int value, Image image)
            : base(x, y, 24, 24, image)
        {
            Value = value;
            IsCollected = false;

            if (image == null && Sprite != null)
            {
                Sprite.BackColor = Color.Gold;
                Sprite.BorderStyle = BorderStyle.FixedSingle;
            }
        }
        public override void Draw(Camera camera)
        {
            if (IsCollected)
            {
                if (Sprite != null)
                {
                    Sprite.Visible = false;
                }
                return;
            }

            base.Draw(camera);
        }

        public override void Update(GameTime gameTime, Camera camera)
        {
            
            Draw(camera);
        }

        
        public virtual void Collect()
        {
            if (!IsCollected)
            {
                IsCollected = true;
                
                if (Sprite != null)
                {
                    Sprite.Visible = false;
                }
            }
        }

        
        public virtual CrystalType Type => CrystalType.Regular;
    }

    
    public class SpecialCrystal : Collectible
    {
        
        private readonly CrystalType _type;

        public SpecialCrystal(float x, float y, CrystalType type, Image image)
            : base(x, y, 25, image) 
        {
            _type = type;
            Value = 25; 

            
            if (image == null && Sprite != null)
            {
                switch (type)
                {
                    case CrystalType.DoubleJump:
                        Sprite.BackColor = Color.DodgerBlue;
                        break;
                    case CrystalType.SpeedBoost:
                        Sprite.BackColor = Color.Lime;
                        break;
                    case CrystalType.Invincibility:
                        Sprite.BackColor = Color.Fuchsia;
                        break;
                    default:
                        Sprite.BackColor = Color.Cyan;
                        break;
                }
                Sprite.BorderStyle = BorderStyle.FixedSingle;
            }
        }

        public override CrystalType Type => _type;
    }

    
    public enum CrystalType
    {
        Regular,
        DoubleJump,
        SpeedBoost,
        Invincibility
    }
}