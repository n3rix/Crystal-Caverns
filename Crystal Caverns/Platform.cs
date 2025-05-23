using System;
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
            // Рассчитываем направление и расстояние
            float targetX = _movingToEnd ? _endPosition.X : _startPosition.X;
            float targetY = _movingToEnd ? _endPosition.Y : _startPosition.Y;
            float dx = targetX - Position.X;
            float dy = targetY - Position.Y;
            float distance = (float)Math.Sqrt(dx * dx + dy * dy);

            // Если мы рядом с целью, меняем направление
            if (distance < 2.0f) // Минимальное расстояние для смены направления
            {
                Position = new PointF(targetX, targetY);
                _movingToEnd = !_movingToEnd;
                VelocityX = 0;
                VelocityY = 0;
            }
            else
            {
                // Нормализуем вектор направления и умножаем на скорость
                float velocityX = dx / distance * _speed;
                float velocityY = dy / distance * _speed;

                // Применяем движение с фиксированным шагом (не зависящим от deltaTime)
                VelocityX = velocityX;
                VelocityY = velocityY;
                Position = new PointF(
                    Position.X + VelocityX,
                    Position.Y + VelocityY
                );
            }

            Draw(camera);
        }
    }
}