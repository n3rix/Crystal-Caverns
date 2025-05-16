using System.Drawing;
using System.Windows.Forms;
using Crystal_Caverns.Utils;

namespace Crystal_Caverns.Model
{
    public abstract class GameObject
    {
        public PointF Position { get; protected set; }
        public SizeF Size { get; protected set; }
        public PictureBox Sprite { get; protected set; }
        public float VelocityX { get; set; }
        public float VelocityY { get; set; }

        public RectangleF Bounds => new RectangleF(Position, Size);
        public bool IsActive { get; set; } = true;

        protected GameObject(float x, float y, float width, float height, Image image = null)
        {
            Position = new PointF(x, y);
            Size = new SizeF(width, height);

            Sprite = new PictureBox
            {
                Location = new Point((int)x, (int)y),
                Size = new Size((int)width, (int)height),
                SizeMode = PictureBoxSizeMode.StretchImage,
                Image = image,
                BackColor = Color.Transparent
            };
        }

        
        public abstract void Update(GameTime gameTime, Camera camera);

        public virtual void Draw(Camera camera)
        {
            if (Sprite != null && !Sprite.IsDisposed)
            {
                
                if (camera.IsInView(Bounds))
                {
                    PointF screenPos = camera.WorldToScreen(Position);

                    Sprite.Location = new Point((int)screenPos.X, (int)screenPos.Y);

                    Sprite.Visible = true; 
                }
                else
                {
                    
                    Sprite.Visible = false;
                }
            }
        }

        
        public virtual bool Intersects(GameObject other)
        {
            
            if (other is Collectible)
            {
                
                RectangleF expandedBounds = new RectangleF(
                    Position.X - 5, Position.Y - 5,
                    Size.Width + 10, Size.Height + 10);
                return expandedBounds.IntersectsWith(other.Bounds);
            }

            return Bounds.IntersectsWith(other.Bounds);
        }

        public virtual void OnCollision(GameObject other) { }

        
        public virtual void Dispose()
        {
            if (Sprite != null && !Sprite.IsDisposed)
            {
                Sprite.Dispose();
            }
        }
    }

    public class GameTime
    {
        public float DeltaTime { get; set; }
        public float TotalTime { get; set; }

        public GameTime()
        {
            DeltaTime = 0;
            TotalTime = 0;
        }
    }
}