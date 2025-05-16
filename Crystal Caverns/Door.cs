using System.Drawing;
using System.Windows.Forms;
using Crystal_Caverns.Utils;

namespace Crystal_Caverns.Model
{
    public class Door : GameObject
    {
        public bool IsOpen { get; private set; }

        private readonly Image _closedImage;
        private readonly Image _openImage;
        
        public Door(float x, float y, Image closedImage, Image openImage = null)
            : base(x, y, 48, 64, closedImage)
        {
            IsOpen = false;
            _closedImage = closedImage;
            _openImage = openImage ?? closedImage; 

            if (closedImage == null && Sprite != null)
            {
                Sprite.BackColor = Color.Brown;
                Sprite.BorderStyle = BorderStyle.FixedSingle;
            }
        }

        public override void Update(GameTime gameTime, Camera camera)
        {
            Draw(camera);
        }

        
        public void Open()
        {
            if (!IsOpen)
            {
                IsOpen = true;

                if (Sprite != null)
                {
                    Sprite.Image = _openImage;

                    
                    if (_openImage == null)
                    {
                        Sprite.BackColor = Color.Green;
                    }
                }
            }
        }

        
        public void Close()
        {
            if (IsOpen)
            {
                IsOpen = false;

                if (Sprite != null)
                {
                    Sprite.Image = _closedImage;

                    if (_closedImage == null)
                    {
                        Sprite.BackColor = Color.Brown;
                    }
                }
            }
        }
    }
}