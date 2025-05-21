using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Crystal_Caverns.Model;

namespace Crystal_Caverns.Utils
{
    // Класс для упрощенного поиска пути в 2D пространстве с поддержкой прыжков
    public class PathFinder
    {
        private readonly List<Platform> _platforms;
        private readonly float _worldWidth;
        private readonly float _worldHeight;
        private readonly int _gridSize = 30; // Размер ячейки сетки
        private GameManager _gameManager;

        // Настройки прыжков
        private const float MAX_JUMP_HEIGHT = 150f;         // Максимальная высота прыжка
        private const float MAX_JUMP_DISTANCE = 200f;       // Максимальное горизонтальное расстояние прыжка
        private const float PREFERRED_JUMP_DISTANCE = 120f; // Предпочтительное расстояние прыжка

        public PathFinder(List<Platform> platforms, float worldWidth = 3000, float worldHeight = 1000)
        {
            _platforms = platforms;
            _worldWidth = worldWidth;
            _worldHeight = worldHeight;
            _gameManager = GameManager.Instance;
        }

        // Поиск пути с учетом прыжков
        public List<PointF> FindPath(PointF start, PointF end, float entityWidth, float entityHeight)
        {
            List<PointF> path = new List<PointF>();

            // Проверка прямого прыжка к игроку
            if (CanJumpDirectlyTo(start, end, entityWidth, entityHeight))
            {
                Console.WriteLine(">>> МОЖЕМ ПРЫГНУТЬ НАПРЯМУЮ К ЦЕЛИ!");
                path.Add(end);
                return path;
            }

            // Проверка прямого пути без прыжка
            float directPathCost;
            if (IsDirectPathPossible(start, end, entityWidth, entityHeight, out directPathCost))
            {
                Console.WriteLine(">>> МОЖЕМ ИДТИ НАПРЯМУЮ К ЦЕЛИ!");
                path.Add(end);
                return path;
            }

            // Если прямые пути невозможны, ищем через промежуточные точки
            return FindPathViaWaypoints(start, end, entityWidth, entityHeight);
        }

        // Проверка, можно ли прыгнуть напрямую к цели
        public bool CanJumpDirectlyTo(PointF start, PointF end, float entityWidth, float entityHeight)
        {
            // Расчет расстояний
            float dx = end.X - start.X;
            float dy = start.Y - end.Y; // Отрицательные значения = игрок выше, положительные = игрок ниже
            float horizontalDist = Math.Abs(dx);

            // Проверки базовых условий для прыжка
            if (horizontalDist > MAX_JUMP_DISTANCE)
                return false; // Слишком далеко по горизонтали

            if (dy < -30)
                return false; // Игрок слишком высоко для прыжка

            if (dy > 100)
                return false; // Игрок слишком низко, упадем на него

            // Проверка наличия платформы под целью
            bool hasTargetGround = IsPlatformUnder(end, 10);
            if (!hasTargetGround)
                return false; // Нет платформы под целью

            // Проверка наличия препятствий на пути прыжка
            if (HasObstaclesInJumpPath(start, end, entityWidth, entityHeight))
                return false; // Что-то мешает прыгнуть

            // Специальная проверка для прыжка на соседнюю платформу
            if (IsOnDifferentPlatforms(start, end) && horizontalDist < MAX_JUMP_DISTANCE)
            {
                Console.WriteLine($">>> Проверка прыжка на другую платформу. Расстояние: {horizontalDist}, Высота: {dy}");
                return true; // Если платформы разные и расстояние в пределах прыжка
            }

            // Если высота подходящая и расстояние в пределах прыжка
            if (Math.Abs(dy) < MAX_JUMP_HEIGHT && horizontalDist < PREFERRED_JUMP_DISTANCE)
            {
                return true;
            }

            return false;
        }

        // Проверка, находятся ли точки на разных платформах
        public bool IsOnDifferentPlatforms(PointF a, PointF b)
        {
            Platform platformA = GetPlatformAt(a);
            Platform platformB = GetPlatformAt(b);

            // Если хотя бы одна точка не на платформе, считаем их на разных платформах
            if (platformA == null || platformB == null)
                return true;

            // Проверяем, одна ли это платформа
            return platformA != platformB;
        }

        // Получение платформы под указанной точкой
        private Platform GetPlatformAt(PointF point)
        {
            RectangleF checkRect = new RectangleF(point.X - 2, point.Y, 4, 20);

            foreach (var platform in _platforms)
            {
                if (checkRect.IntersectsWith(platform.Bounds))
                    return platform;
            }

            return null;
        }

        // Проверка наличия платформы под точкой
        private bool IsPlatformUnder(PointF point, float checkDistance)
        {
            RectangleF checkRect = new RectangleF(point.X - 5, point.Y, 10, checkDistance);

            foreach (var platform in _platforms)
            {
                if (checkRect.IntersectsWith(platform.Bounds))
                    return true;
            }

            return false;
        }

        // Проверка наличия препятствий на пути прыжка
        private bool HasObstaclesInJumpPath(PointF start, PointF end, float entityWidth, float entityHeight)
        {
            // Рассчитываем траекторию прыжка (упрощенно)
            float dx = end.X - start.X;
            float distance = Math.Abs(dx);
            int steps = Math.Max(5, (int)(distance / 20));

            // Примерная высота прыжка (упрощенный парабола)
            float maxJumpHeight = Math.Min(50, Math.Abs(start.Y - end.Y) + 30);

            for (int i = 1; i < steps; i++)
            {
                float t = i / (float)steps;

                // Расчет точки на траектории (параболическая траектория)
                float x = start.X + dx * t;
                float y = start.Y - maxJumpHeight * 4 * t * (1 - t); // Парабола для прыжка

                if (end.Y < start.Y)
                {
                    // Если конечная точка выше, корректируем
                    float heightDiff = start.Y - end.Y;
                    y -= heightDiff * t;
                }

                // Создаем прямоугольник для проверки столкновений
                RectangleF entityRect = new RectangleF(
                    x - entityWidth / 2,
                    y - entityHeight / 2,
                    entityWidth,
                    entityHeight
                );

                // Проверяем столкновения с платформами
                foreach (var platform in _platforms)
                {
                    if (entityRect.IntersectsWith(platform.Bounds))
                    {
                        // Если это конец и платформа под конечной точкой - это не препятствие
                        if (i == steps - 1 && platform.Position.Y >= end.Y)
                            continue;

                        // Если это начало и платформа под начальной точкой - это не препятствие
                        if (i == 1 && platform.Position.Y >= start.Y)
                            continue;

                        return true; // Есть препятствие
                    }
                }
            }

            return false; // Нет препятствий
        }

        // Метод для поиска пути через промежуточные точки
        private List<PointF> FindPathViaWaypoints(PointF start, PointF end, float entityWidth, float entityHeight)
        {
            List<PointF> path = new List<PointF>();
            List<PointF> waypoints = FindPossibleWaypoints(start, end);

            if (waypoints.Count == 0)
            {
                // Если нет промежуточных точек, просто возвращаем конечную точку
                path.Add(end);
                return path;
            }

            // Поиск лучшего пути через промежуточные точки
            PointF bestWaypoint = PointF.Empty;
            float bestScore = float.MaxValue;
            bool canJumpToBest = false;

            foreach (var waypoint in waypoints)
            {
                bool canJump = CanJumpDirectlyTo(start, waypoint, entityWidth, entityHeight);
                float directDistance = CalculateDistance(waypoint, end);

                // Приоритет точкам, к которым можно прыгнуть
                float score = canJump ? directDistance * 0.7f : directDistance;

                // Предпочитаем точки на пути к цели
                if (IsPointTowardsTarget(start, end, waypoint))
                    score *= 0.8f;

                // Предпочитаем точки выше текущей позиции
                if (waypoint.Y < start.Y)
                    score *= 0.9f;

                if (score < bestScore)
                {
                    bestScore = score;
                    bestWaypoint = waypoint;
                    canJumpToBest = canJump;
                }
            }

            if (bestScore != float.MaxValue)
            {
                // Добавляем лучшую промежуточную точку
                path.Add(bestWaypoint);
                Console.WriteLine($">>> Найдена хорошая промежуточная точка: {bestWaypoint.X}, {bestWaypoint.Y}");

                // Отмечаем, если к ней можно прыгнуть
                if (canJumpToBest)
                {
                    Console.WriteLine(">>> К этой точке можно прыгнуть!");
                }

                // Добавляем конечную точку
                path.Add(end);
            }
            else
            {
                // Если не нашли подходящих промежуточных точек, возвращаем конечную
                path.Add(end);
            }

            return path;
        }

        // Проверка, находится ли точка в направлении к цели
        private bool IsPointTowardsTarget(PointF start, PointF target, PointF point)
        {
            // Вектор от начала к цели
            float targetDx = target.X - start.X;
            float targetDy = target.Y - start.Y;

            // Вектор от начала к точке
            float pointDx = point.X - start.X;
            float pointDy = point.Y - start.Y;

            // Проверка направления по X
            if (targetDx > 0 && pointDx < 0) return false;
            if (targetDx < 0 && pointDx > 0) return false;

            // Проверка расстояния
            if (Math.Abs(pointDx) > Math.Abs(targetDx) * 1.2f) return false;

            return true;
        }

        // Метод для оценки прямого пути к цели
        public bool IsDirectPathPossible(PointF start, PointF end, float entityWidth, float entityHeight, out float pathCost)
        {
            pathCost = float.MaxValue;

            // Проверка на препятствия по прямой линии
            if (IsPathBlocked(start, end, entityWidth, entityHeight))
                return false;

            // Проверка на возможные пропасти на пути
            if (HasUnpassableGaps(start, end, entityWidth))
                return false;

            // Проверка на большую разницу в высоте (недостижимо без прыжка)
            float verticalDiff = start.Y - end.Y;
            if (verticalDiff > 20) // Если нужно идти вверх, скорее всего нужен прыжок
                return false;

            // Если прямой путь возможен, рассчитываем его стоимость
            float distance = CalculateDistance(start, end);
            pathCost = distance;

            return true;
        }

        // Проверка блокировки пути препятствиями
        private bool IsPathBlocked(PointF start, PointF end, float entityWidth, float entityHeight)
        {
            float dx = end.X - start.X;
            float dy = end.Y - start.Y;
            float distance = (float)Math.Sqrt(dx * dx + dy * dy);

            if (distance < 0.001f)
                return false;

            // Нормализованный вектор направления
            float nx = dx / distance;
            float ny = dy / distance;

            // Минимальное количество шагов для проверки
            int steps = Math.Max(10, (int)(distance / 20));

            for (int i = 1; i < steps; i++)
            {
                float t = i / (float)steps;
                PointF checkPoint = new PointF(
                    start.X + nx * distance * t,
                    start.Y + ny * distance * t
                );

                // Создаем прямоугольник для проверки столкновений
                RectangleF entityRect = new RectangleF(
                    checkPoint.X - entityWidth / 2,
                    checkPoint.Y - entityHeight / 2,
                    entityWidth,
                    entityHeight
                );

                // Проверяем столкновения с платформами
                foreach (var platform in _platforms)
                {
                    // Проверяем только достаточно толстые препятствия
                    if (platform.Size.Height > 15 && entityRect.IntersectsWith(platform.Bounds))
                    {
                        // Если конечная точка близко, не считаем это блокировкой
                        float remainingDistance = CalculateDistance(checkPoint, end);
                        if (remainingDistance < entityWidth)
                            continue;

                        return true; // Путь заблокирован
                    }
                }
            }

            return false;
        }

        // Проверка наличия непреодолимых пропастей на пути
        private bool HasUnpassableGaps(PointF start, PointF end, float entityWidth)
        {
            // Только для горизонтальных путей
            if (Math.Abs(end.Y - start.Y) > 30)
                return false;

            int direction = start.X < end.X ? 1 : -1;
            float distance = Math.Abs(end.X - start.X);

            if (distance < entityWidth * 2)
                return false; // Слишком близко для пропасти

            // Проверяем есть ли пропасти на пути
            int steps = Math.Max(5, (int)(distance / 50));
            float stepSize = distance / steps;

            for (int i = 1; i < steps; i++)
            {
                float testX = start.X + i * direction * stepSize;

                // Проверяем наличие опоры под этой точкой
                bool hasGround = CheckGroundAt(testX, Math.Max(start.Y, end.Y) + 10);

                if (!hasGround)
                {
                    // Измеряем ширину пропасти
                    float gapWidth = MeasureGapWidth(testX, Math.Max(start.Y, end.Y) + 10, direction);

                    // Если пропасть слишком широкая для прыжка
                    float maxJumpWidth = 150; // Максимальная ширина пропасти для обычного перехода
                    if (gapWidth > maxJumpWidth)
                        return true;
                }
            }

            return false;
        }

        // Проверка наличия земли в указанной точке
        private bool CheckGroundAt(float x, float y)
        {
            // Создаем прямоугольник для проверки
            RectangleF checkRect = new RectangleF(x - 5, y, 10, 40);

            foreach (var platform in _platforms)
            {
                if (checkRect.IntersectsWith(platform.Bounds))
                    return true;
            }

            return false;
        }

        // Измерение ширины пропасти
        private float MeasureGapWidth(float startX, float y, int direction)
        {
            const float CHECK_STEP = 10f;
            const float MAX_CHECK_DISTANCE = 300f;

            bool foundGround = false;
            float distance = 0;

            for (float testX = startX;
                distance <= MAX_CHECK_DISTANCE;
                testX += direction * CHECK_STEP, distance += CHECK_STEP)
            {
                if (CheckGroundAt(testX, y))
                {
                    foundGround = true;
                    break;
                }
            }

            return foundGround ? distance : MAX_CHECK_DISTANCE;
        }

        // Расчет дистанции между двумя точками
        private float CalculateDistance(PointF a, PointF b)
        {
            float dx = b.X - a.X;
            float dy = b.Y - a.Y;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        // Находит возможные промежуточные точки на пути к цели
        public List<PointF> FindPossibleWaypoints(PointF start, PointF end)
        {
            List<PointF> waypoints = new List<PointF>();

            // Добавляем платформы как потенциальные точки пути
            foreach (var platform in _platforms)
            {
                // Создаем точки на краях платформы
                AddPlatformWaypoints(platform, start, end, waypoints);
            }

            // Сортируем точки по расстоянию до цели и направлению
            waypoints = waypoints
                .OrderBy(wp => {
                    float dist = CalculateDistance(wp, end);

                    // Приоритет точкам в нужном направлении
                    if (!IsPointTowardsTarget(start, end, wp))
                        dist *= 1.5f;

                    return dist;
                })
                .ToList();

            // Ограничиваем количество промежуточных точек
            return waypoints.Take(10).ToList();
        }

        // Добавляет точки платформы как возможные промежуточные точки
        private void AddPlatformWaypoints(Platform platform, PointF start, PointF end, List<PointF> waypoints)
        {
            float platformLeft = platform.Position.X;
            float platformRight = platform.Position.X + platform.Size.Width;
            float platformTop = platform.Position.Y;

            // Расстояние от начала до платформы
            float distToPlatform = Math.Min(
                Math.Abs(start.X - platformLeft),
                Math.Abs(start.X - platformRight)
            );

            // Если платформа слишком далеко, пропускаем
            if (distToPlatform > 400)
                return;

            // Главное - разместить точки на верхних углах платформы
            PointF leftTop = new PointF(platformLeft + 5, platformTop - 5);
            PointF rightTop = new PointF(platformRight - 5, platformTop - 5);

            // Добавляем точки в список, если они достаточно далеко от начала
            if (CalculateDistance(start, leftTop) > 20)
                waypoints.Add(leftTop);

            if (CalculateDistance(start, rightTop) > 20)
                waypoints.Add(rightTop);

            // Добавляем дополнительную точку в центре, если платформа широкая
            if (platform.Size.Width > 100)
            {
                PointF centerTop = new PointF(platformLeft + platform.Size.Width / 2, platformTop - 5);
                waypoints.Add(centerTop);
            }
        }
    }
}