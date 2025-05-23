using System;
using System.Drawing;
using Crystal_Caverns.Model;

namespace Crystal_Caverns.Utils
{
    public class Camera
    {

        public PointF Position { get; private set; }

        public SizeF ViewportSize { get; private set; }

        public SizeF WorldSize { get; private set; }

        private GameObject _target;

        private float _smoothingFactor = 0.1f;

        // Добавляем поля для динамических границ мира
        private float _minWorldY = float.MaxValue;
        private float _maxWorldY = float.MinValue;
        private float _minWorldX = float.MaxValue;
        private float _maxWorldX = float.MinValue;
        private bool _boundsCalculated = false;

        public Camera(SizeF viewportSize, SizeF worldSize)
        {
            ViewportSize = viewportSize;
            WorldSize = worldSize;
            Position = new PointF(0, 0);
        }

        public void SetTarget(GameObject target)
        {
            _target = target;
        }

        // Метод для расчета реальных границ мира на основе объектов
        public void CalculateWorldBounds()
        {
            var gameManager = GameManager.Instance;
            if (gameManager == null) return;

            var gameObjects = gameManager.GetGameObjects();

            _minWorldY = float.MaxValue;
            _maxWorldY = float.MinValue;
            _minWorldX = float.MaxValue;
            _maxWorldX = float.MinValue;

            foreach (var obj in gameObjects)
            {
                if (obj == null || !obj.IsActive) continue;

                // Обновляем границы на основе позиции объектов
                float objLeft = obj.Position.X;
                float objRight = obj.Position.X + obj.Size.Width;
                float objTop = obj.Position.Y;
                float objBottom = obj.Position.Y + obj.Size.Height;

                _minWorldX = Math.Min(_minWorldX, objLeft);
                _maxWorldX = Math.Max(_maxWorldX, objRight);
                _minWorldY = Math.Min(_minWorldY, objTop);
                _maxWorldY = Math.Max(_maxWorldY, objBottom);
            }

            // Добавляем буферную зону для удобства навигации
            const float buffer = 100f;
            _minWorldY -= buffer;
            _maxWorldY += buffer;
            _minWorldX -= buffer;
            _maxWorldX += buffer;

            // Убеждаемся, что границы не меньше оригинального размера мира
            _minWorldX = Math.Min(_minWorldX, 0);
            _maxWorldX = Math.Max(_maxWorldX, WorldSize.Width);
            _minWorldY = Math.Min(_minWorldY, 0);
            _maxWorldY = Math.Max(_maxWorldY, WorldSize.Height);

            _boundsCalculated = true;

            Console.WriteLine($"Рассчитанные границы мира: X({_minWorldX:F0} - {_maxWorldX:F0}), Y({_minWorldY:F0} - {_maxWorldY:F0})");
        }

        public void Update(GameTime gameTime)
        {
            if (_target == null) return;

            // Пересчитываем границы мира, если они еще не рассчитаны
            if (!_boundsCalculated)
            {
                CalculateWorldBounds();
            }

            float targetX = _target.Position.X + (_target.Size.Width / 2) - (ViewportSize.Width / 2);
            float targetY = _target.Position.Y + (_target.Size.Height / 2) - (ViewportSize.Height / 2);

            Position = new PointF(
                Position.X + (_smoothingFactor * (targetX - Position.X)),
                Position.Y + (_smoothingFactor * (targetY - Position.Y))
            );

            // Используем динамические границы вместо статических
            float minCameraX = _minWorldX;
            float maxCameraX = _maxWorldX - ViewportSize.Width;
            float minCameraY = _minWorldY;
            float maxCameraY = _maxWorldY - ViewportSize.Height;

            Position = new PointF(
                Math.Max(minCameraX, Math.Min(Position.X, maxCameraX)),
                Math.Max(minCameraY, Math.Min(Position.Y, maxCameraY))
            );
        }

        // Метод для принудительного пересчета границ (можно вызывать при добавлении новых объектов)
        public void RecalculateBounds()
        {
            _boundsCalculated = false;
        }

        public PointF WorldToScreen(PointF worldPosition)
        {
            return new PointF(
                worldPosition.X - Position.X,
                worldPosition.Y - Position.Y
            );
        }

        public PointF ScreenToWorld(PointF screenPosition)
        {
            return new PointF(
                screenPosition.X + Position.X,
                screenPosition.Y + Position.Y
            );
        }

        public bool IsInView(RectangleF objectBounds)
        {
            RectangleF viewRect = new RectangleF(Position, ViewportSize);
            return viewRect.IntersectsWith(objectBounds);
        }

        // Дополнительные методы для отладки
        public void PrintCameraInfo()
        {
            Console.WriteLine($"Позиция камеры: X={Position.X:F1}, Y={Position.Y:F1}");
            Console.WriteLine($"Границы мира: X({_minWorldX:F0} - {_maxWorldX:F0}), Y({_minWorldY:F0} - {_maxWorldY:F0})");
            if (_target != null)
            {
                Console.WriteLine($"Позиция цели: X={_target.Position.X:F1}, Y={_target.Position.Y:F1}");
            }
        }
    }
}