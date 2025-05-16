using System.Drawing;
using Crystal_Caverns.Utils;

namespace Crystal_Caverns.Model
{
    public class Platform : GameObject
    {
        public Platform(float x, float y, float width, float height, Image image)
            : base(x, y, width, height, image)
        {
            if (image == null && Sprite != null)
            {
                Sprite.BackColor = Color.SaddleBrown;
            }
        }

        public override void Update(GameTime gameTime, Camera camera)
        {
            Draw(camera);
        }
    }

    public class MovingPlatform : Platform
    {

        private readonly PointF _startPosition;
        private readonly PointF _endPosition;
        private readonly float _speed;
        private bool _movingToEnd;

        public MovingPlatform(float x, float y, float width, float height, PointF endPosition, float speed, Image image)
            : base(x, y, width, height, image)
        {
            _startPosition = new PointF(x, y);
            _endPosition = endPosition;
            _speed = speed;
            _movingToEnd = true;

            if (image == null && Sprite != null)
            {
                Sprite.BackColor = Color.Peru;
            }
        }

        public override void Update(GameTime gameTime, Camera camera)
        {

            if (_movingToEnd)
            {

                if (IsHorizontalMovement)
                {

                    VelocityX = _speed;
                    if (Position.X >= _endPosition.X)
                    {
                        _movingToEnd = false;
                    }
                }
                else
                {
                    VelocityY = _speed;
                    if (Position.Y >= _endPosition.Y)
                    {
                        _movingToEnd = false;
                    }
                }
            }
            else
            {
                if (IsHorizontalMovement)
                {
                    VelocityX = -_speed;
                    if (Position.X <= _startPosition.X)
                    {
                        _movingToEnd = true;
                    }
                }
                else
                {
                    VelocityY = -_speed;
                    if (Position.Y <= _startPosition.Y)
                    {
                        _movingToEnd = true;
                    }
                }
            }


            Position = new PointF(Position.X + VelocityX, Position.Y + VelocityY);

            Draw(camera);
        }


        private bool IsHorizontalMovement => _startPosition.Y == _endPosition.Y;
    }
}