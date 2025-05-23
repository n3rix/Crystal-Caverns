using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Crystal_Caverns.Model;

namespace Crystal_Caverns.Utils
{
    public class PathFinder
    {
        private readonly List<Platform> _platforms;
        private readonly float _worldWidth;
        private readonly float _worldHeight;
        private readonly int _gridSize = 30; private GameManager _gameManager;

        private const float MAX_JUMP_HEIGHT = 150f; private const float MAX_JUMP_DISTANCE = 200f; private const float PREFERRED_JUMP_DISTANCE = 120f;
        public PathFinder(List<Platform> platforms, float worldWidth = 3000, float worldHeight = 1000)
        {
            _platforms = platforms;
            _worldWidth = worldWidth;
            _worldHeight = worldHeight;
            _gameManager = GameManager.Instance;
        }

        public List<PointF> FindPath(PointF start, PointF end, float entityWidth, float entityHeight)
        {
            List<PointF> path = new List<PointF>();

            if (CanJumpDirectlyTo(start, end, entityWidth, entityHeight))
            {
                Console.WriteLine(">>> МОЖЕМ ПРЫГНУТЬ НАПРЯМУЮ К ЦЕЛИ!");
                path.Add(end);
                return path;
            }

            float directPathCost;
            if (IsDirectPathPossible(start, end, entityWidth, entityHeight, out directPathCost))
            {
                Console.WriteLine(">>> МОЖЕМ ИДТИ НАПРЯМУЮ К ЦЕЛИ!");
                path.Add(end);
                return path;
            }

            return FindPathViaWaypoints(start, end, entityWidth, entityHeight);
        }

        public bool CanJumpDirectlyTo(PointF start, PointF end, float entityWidth, float entityHeight)
        {
            float dx = end.X - start.X;
            float dy = start.Y - end.Y; float horizontalDist = Math.Abs(dx);

            if (horizontalDist > MAX_JUMP_DISTANCE)
                return false;
            if (dy < -30)
                return false;
            if (dy > 100)
                return false;
            bool hasTargetGround = IsPlatformUnder(end, 10);
            if (!hasTargetGround)
                return false;
            if (HasObstaclesInJumpPath(start, end, entityWidth, entityHeight))
                return false;
            if (IsOnDifferentPlatforms(start, end) && horizontalDist < MAX_JUMP_DISTANCE)
            {
                Console.WriteLine($">>> Проверка прыжка на другую платформу. Расстояние: {horizontalDist}, Высота: {dy}");
                return true;
            }

            if (Math.Abs(dy) < MAX_JUMP_HEIGHT && horizontalDist < PREFERRED_JUMP_DISTANCE)
            {
                return true;
            }

            return false;
        }

        public bool IsOnDifferentPlatforms(PointF a, PointF b)
        {
            Platform platformA = GetPlatformAt(a);
            Platform platformB = GetPlatformAt(b);

            if (platformA == null || platformB == null)
                return true;

            return platformA != platformB;
        }

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

        private bool HasObstaclesInJumpPath(PointF start, PointF end, float entityWidth, float entityHeight)
        {
            float dx = end.X - start.X;
            float distance = Math.Abs(dx);
            int steps = Math.Max(5, (int)(distance / 20));

            float maxJumpHeight = Math.Min(50, Math.Abs(start.Y - end.Y) + 30);

            for (int i = 1; i < steps; i++)
            {
                float t = i / (float)steps;

                float x = start.X + dx * t;
                float y = start.Y - maxJumpHeight * 4 * t * (1 - t);
                if (end.Y < start.Y)
                {
                    float heightDiff = start.Y - end.Y;
                    y -= heightDiff * t;
                }

                RectangleF entityRect = new RectangleF(
    x - entityWidth / 2,
    y - entityHeight / 2,
    entityWidth,
    entityHeight
);

                foreach (var platform in _platforms)
                {
                    if (entityRect.IntersectsWith(platform.Bounds))
                    {
                        if (i == steps - 1 && platform.Position.Y >= end.Y)
                            continue;

                        if (i == 1 && platform.Position.Y >= start.Y)
                            continue;

                        return true;
                    }
                }
            }

            return false;
        }

        private List<PointF> FindPathViaWaypoints(PointF start, PointF end, float entityWidth, float entityHeight)
        {
            List<PointF> path = new List<PointF>();
            List<PointF> waypoints = FindPossibleWaypoints(start, end);

            if (waypoints.Count == 0)
            {
                path.Add(end);
                return path;
            }

            PointF bestWaypoint = PointF.Empty;
            float bestScore = float.MaxValue;
            bool canJumpToBest = false;

            foreach (var waypoint in waypoints)
            {
                bool canJump = CanJumpDirectlyTo(start, waypoint, entityWidth, entityHeight);
                float directDistance = CalculateDistance(waypoint, end);

                float score = canJump ? directDistance * 0.7f : directDistance;

                if (IsPointTowardsTarget(start, end, waypoint))
                    score *= 0.8f;

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
                path.Add(bestWaypoint);
                Console.WriteLine($">>> Найдена хорошая промежуточная точка: {bestWaypoint.X}, {bestWaypoint.Y}");

                if (canJumpToBest)
                {
                    Console.WriteLine(">>> К этой точке можно прыгнуть!");
                }

                path.Add(end);
            }
            else
            {
                path.Add(end);
            }

            return path;
        }

        private bool IsPointTowardsTarget(PointF start, PointF target, PointF point)
        {
            float targetDx = target.X - start.X;
            float targetDy = target.Y - start.Y;

            float pointDx = point.X - start.X;
            float pointDy = point.Y - start.Y;

            if (targetDx > 0 && pointDx < 0) return false;
            if (targetDx < 0 && pointDx > 0) return false;

            if (Math.Abs(pointDx) > Math.Abs(targetDx) * 1.2f) return false;

            return true;
        }

        public bool IsDirectPathPossible(PointF start, PointF end, float entityWidth, float entityHeight, out float pathCost)
        {
            pathCost = float.MaxValue;

            if (IsPathBlocked(start, end, entityWidth, entityHeight))
                return false;

            if (HasUnpassableGaps(start, end, entityWidth))
                return false;

            float verticalDiff = start.Y - end.Y;
            if (verticalDiff > 20) return false;

            float distance = CalculateDistance(start, end);
            pathCost = distance;

            return true;
        }

        private bool IsPathBlocked(PointF start, PointF end, float entityWidth, float entityHeight)
        {
            float dx = end.X - start.X;
            float dy = end.Y - start.Y;
            float distance = (float)Math.Sqrt(dx * dx + dy * dy);

            if (distance < 0.001f)
                return false;

            float nx = dx / distance;
            float ny = dy / distance;

            int steps = Math.Max(10, (int)(distance / 20));

            for (int i = 1; i < steps; i++)
            {
                float t = i / (float)steps;
                PointF checkPoint = new PointF(
                    start.X + nx * distance * t,
                    start.Y + ny * distance * t
                );

                RectangleF entityRect = new RectangleF(
    checkPoint.X - entityWidth / 2,
    checkPoint.Y - entityHeight / 2,
    entityWidth,
    entityHeight
);

                foreach (var platform in _platforms)
                {
                    if (platform.Size.Height > 15 && entityRect.IntersectsWith(platform.Bounds))
                    {
                        float remainingDistance = CalculateDistance(checkPoint, end);
                        if (remainingDistance < entityWidth)
                            continue;

                        return true;
                    }
                }
            }

            return false;
        }

        private bool HasUnpassableGaps(PointF start, PointF end, float entityWidth)
        {
            if (Math.Abs(end.Y - start.Y) > 30)
                return false;

            int direction = start.X < end.X ? 1 : -1;
            float distance = Math.Abs(end.X - start.X);

            if (distance < entityWidth * 2)
                return false;
            int steps = Math.Max(5, (int)(distance / 50));
            float stepSize = distance / steps;

            for (int i = 1; i < steps; i++)
            {
                float testX = start.X + i * direction * stepSize;

                bool hasGround = CheckGroundAt(testX, Math.Max(start.Y, end.Y) + 10);

                if (!hasGround)
                {
                    float gapWidth = MeasureGapWidth(testX, Math.Max(start.Y, end.Y) + 10, direction);

                    float maxJumpWidth = 150; if (gapWidth > maxJumpWidth)
                        return true;
                }
            }

            return false;
        }

        private bool CheckGroundAt(float x, float y)
        {
            RectangleF checkRect = new RectangleF(x - 5, y, 10, 40);

            foreach (var platform in _platforms)
            {
                if (checkRect.IntersectsWith(platform.Bounds))
                    return true;
            }

            return false;
        }

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

        private float CalculateDistance(PointF a, PointF b)
        {
            float dx = b.X - a.X;
            float dy = b.Y - a.Y;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        public List<PointF> FindPossibleWaypoints(PointF start, PointF end)
        {
            List<PointF> waypoints = new List<PointF>();

            foreach (var platform in _platforms)
            {
                AddPlatformWaypoints(platform, start, end, waypoints);
            }

            waypoints = waypoints
    .OrderBy(wp =>
    {
        float dist = CalculateDistance(wp, end);

        if (!IsPointTowardsTarget(start, end, wp))
            dist *= 1.5f;

        return dist;
    })
    .ToList();

            return waypoints.Take(10).ToList();
        }

        private void AddPlatformWaypoints(Platform platform, PointF start, PointF end, List<PointF> waypoints)
        {
            float platformLeft = platform.Position.X;
            float platformRight = platform.Position.X + platform.Size.Width;
            float platformTop = platform.Position.Y;

            float distToPlatform = Math.Min(
    Math.Abs(start.X - platformLeft),
    Math.Abs(start.X - platformRight)
);

            if (distToPlatform > 400)
                return;

            PointF leftTop = new PointF(platformLeft + 5, platformTop - 5);
            PointF rightTop = new PointF(platformRight - 5, platformTop - 5);

            if (CalculateDistance(start, leftTop) > 20)
                waypoints.Add(leftTop);

            if (CalculateDistance(start, rightTop) > 20)
                waypoints.Add(rightTop);

            if (platform.Size.Width > 100)
            {
                PointF centerTop = new PointF(platformLeft + platform.Size.Width / 2, platformTop - 5);
                waypoints.Add(centerTop);
            }
        }
    }
}