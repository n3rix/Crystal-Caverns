using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Crystal_Caverns.Utils;

namespace Crystal_Caverns.Model
{
    public abstract class Enemy : GameObject
    {
        protected readonly Player _player;
        protected readonly float _speed;
        protected readonly PointF _initialPosition;

        protected Enemy(float x, float y, float width, float height, Player player, float speed, Image image)
            : base(x, y, width, height, image)
        {
            _player = player;
            _speed = speed;
            _initialPosition = new PointF(x, y);

            if (image == null && Sprite != null)
            {
                Sprite.BackColor = Color.Red;
                Sprite.BorderStyle = BorderStyle.FixedSingle;
            }
        }
        protected virtual bool IsOnGround()
        {
            RectangleF testRect = new RectangleF(
                Position.X, Position.Y + Size.Height + 1,
                Size.Width, 2);

            var platforms = GameManager.Instance.GetPlatforms();
            foreach (var platform in platforms)
            {
                if (testRect.IntersectsWith(platform.Bounds))
                    return true;
            }
            return false;
        }

        protected bool DetectPlayer(float detectionRange)
        {
            if (_player == null) return false;

            float distanceX = Math.Abs(_player.Position.X - Position.X);
            float distanceY = Math.Abs(_player.Position.Y - Position.Y);

            return distanceX < detectionRange && distanceY < detectionRange * 0.75f;
        }

        public override void OnCollision(GameObject other)
        {
            if (other is Platform platform)
            {
                HandlePlatformCollision(platform);
            }
        }
        protected bool IsJumping()
        {
            return !IsOnGround() && VelocityY < 0;
        }

        protected virtual bool IsInJumpOverGapMode()
        {
            return false;
        }
        protected virtual void HandlePlatformCollision(Platform platform)
        {
            RectangleF enemyRect = Bounds;
            RectangleF platformRect = platform.Bounds;
            bool isAbovePlatform;

            if (VelocityY < 0)
            {
                if (enemyRect.Top < platformRect.Bottom &&
                    enemyRect.Top > platformRect.Top &&
                    enemyRect.Bottom > platformRect.Bottom &&
                    enemyRect.Right > platformRect.Left + 5 &&
                    enemyRect.Left < platformRect.Right - 5)
                {
                    Position = new PointF(Position.X, platformRect.Bottom);
                    VelocityY = 0.5f; Console.WriteLine("Удар головой о платформу!");
                    return;
                }
            }

            if (IsInJumpOverGapMode())
            {
                isAbovePlatform = enemyRect.Bottom >= platformRect.Top &&
                                      enemyRect.Bottom - VelocityY <= platformRect.Top + 5 &&
                                      enemyRect.Right > platformRect.Left + 5 &&
                                      enemyRect.Left < platformRect.Right - 5;

                if (isAbovePlatform && VelocityY >= 0)
                {
                    Position = new PointF(Position.X, platformRect.Top - Size.Height);
                    VelocityY = 0;
                }
                return;
            }

            if (IsJumping())
            {
                isAbovePlatform = enemyRect.Bottom >= platformRect.Top &&
                                       enemyRect.Bottom - VelocityY <= platformRect.Top + 5 &&
                                       enemyRect.Right > platformRect.Left + 5 &&
                                       enemyRect.Left < platformRect.Right - 5;

                if (isAbovePlatform && VelocityY >= 0)
                {
                    Position = new PointF(Position.X, platformRect.Top - Size.Height);
                    VelocityY = 0;
                }
                return;
            }

            isAbovePlatform = enemyRect.Bottom >= platformRect.Top &&
                    enemyRect.Bottom - VelocityY <= platformRect.Top + 5 &&
                    enemyRect.Right > platformRect.Left + 5 &&
                    enemyRect.Left < platformRect.Right - 5;

            if (isAbovePlatform && VelocityY >= 0)
            {
                Position = new PointF(Position.X, platformRect.Top - Size.Height);
                VelocityY = 0;
            }
            else if (VelocityX > 0 && enemyRect.Right > platformRect.Left &&
                    enemyRect.Left < platformRect.Left)
            {
                Position = new PointF(platformRect.Left - Size.Width, Position.Y);
                VelocityX = -VelocityX;
            }
            else if (VelocityX < 0 && enemyRect.Left < platformRect.Right &&
                    enemyRect.Right > platformRect.Right)
            {
                Position = new PointF(platformRect.Right, Position.Y);
                VelocityX = -VelocityX;
            }
        }

        public virtual void Reset(Camera camera)
        {
            Position = new PointF(_initialPosition.X, _initialPosition.Y);
            VelocityX = 0;
            VelocityY = 0;
            Draw(camera);
        }
    }

    public class PatrolEnemy : Enemy
    {
        private readonly float _patrolDistance;
        private bool _movingRight;

        public PatrolEnemy(float x, float y, Player player, float speed, float patrolDistance, Image image)
            : base(x, y, 32, 32, player, speed, image)
        {
            _patrolDistance = patrolDistance;
            _movingRight = true;

            if (image == null && Sprite != null)
            {
                Sprite.BackColor = Color.OrangeRed;
            }
        }

        public override void Update(GameTime gameTime, Camera camera)
        {
            if (_movingRight)
            {
                VelocityX = _speed;

                if (Position.X >= _initialPosition.X + _patrolDistance)
                {
                    _movingRight = false;
                }
            }
            else
            {
                VelocityX = -_speed;

                if (Position.X <= _initialPosition.X)
                {
                    _movingRight = true;
                }
            }

            VelocityY += 0.5f;
            if (VelocityY > 10f)
            {
                VelocityY = 10f;
            }

            Position = new PointF(Position.X + VelocityX, Position.Y + VelocityY);

            CheckFallReset(camera);

            Draw(camera);
        }

        private void CheckFallReset(Camera camera)
        {
            if (Position.Y > 800)
            {
                Reset(camera);
            }
        }

        protected void CheckPlatformEdge()
        {
            float probeX = _movingRight ?
                Position.X + Size.Width + 5 :
                Position.X - 5;

            float probeY = Position.Y + Size.Height + 5;

            bool foundGround = false;

            if (!foundGround)
            {
                _movingRight = !_movingRight;
            }
        }
    }

    public enum EnemyState
    {
        Patrolling, Chasing, Jumping, Recovery, Idle
    }

    public class ChasingEnemySettings
    {
        public float ChaseSpeed { get; set; } = 3.0f;
        public float PatrolSpeed { get; set; } = 1.5f;
        public float DetectionRange { get; set; } = 300.0f;

        public float JumpStrength { get; set; } = 12.0f;
        public float JumpSpeedBoost { get; set; } = 2f;
        public float JumpCooldown { get; set; } = 0.5f;

        public float IdleTime { get; set; } = 1.0f;
        public float RecoveryTime { get; set; } = 0.5f;
        public float MemoryDuration { get; set; } = 2.0f;
        public float IdleProbability { get; set; } = 0.05f;
    }

    public class ChasingEnemy : Enemy
    {
        private EnemyState _currentState = EnemyState.Patrolling;
        private EnemyState _previousState;
        private readonly ChasingEnemySettings _settings;
        private readonly GameManager _gameManager;

        private float _stateTimer = 0f;
        private float _jumpCooldown = 0f;
        private float _gapWidth = 0f;
        private int _breathingOffset = 0;
        private bool _movingRight = true;
        private static readonly Random _random = new Random();

        private bool _hasLastKnownPlayerPosition = false;
        private PointF _lastKnownPlayerPosition;
        private float _memoryTimer = 0f;

        private Utils.PathFinder _pathFinder;
        private List<PointF> _currentPath = new List<PointF>();
        private int _currentPathIndex = -1;
        private float _pathUpdateTimer = 0f;
        private const float PATH_UPDATE_INTERVAL = 0.5f; private bool _jumpToTarget = false;
        public ChasingEnemy(float x, float y, Player player, float speed, float detectionRange, Image image)
            : base(x, y, 32, 32, player, speed, image)
        {
            _gameManager = GameManager.Instance;
            _settings = new ChasingEnemySettings
            {
                ChaseSpeed = speed,
                PatrolSpeed = speed * 0.5f,
                DetectionRange = detectionRange
            };

            _pathFinder = new Utils.PathFinder(_gameManager.GetPlatforms().ToList());

            if (image == null && Sprite != null)
            {
                Sprite.BackColor = Color.Crimson;
            }

            Console.WriteLine("Создан преследующий враг с системой поиска пути");
        }

        public override void Update(GameTime gameTime, Camera camera)
        {
            UpdateTimers(gameTime);

            switch (_currentState)
            {
                case EnemyState.Patrolling:
                    UpdatePatrolState(gameTime);
                    break;
                case EnemyState.Chasing:
                    UpdateChaseState(gameTime);
                    break;
                case EnemyState.Jumping:
                    UpdateJumpState(gameTime);
                    break;
                case EnemyState.Recovery:
                    UpdateRecoveryState(gameTime);
                    break;
                case EnemyState.Idle:
                    UpdateIdleState(gameTime);
                    break;
            }

            ApplyGravity();

            Position = new PointF(Position.X + VelocityX, Position.Y + VelocityY);

            CheckWorldBounds();

            if (Position.Y > 1000)
            {
                Reset(camera);
                ChangeState(EnemyState.Patrolling);
            }

            Draw(camera);
        }

        private void UpdateTimers(GameTime gameTime)
        {
            if (_jumpCooldown > 0)
                _jumpCooldown -= gameTime.DeltaTime;

            if (_stateTimer > 0)
                _stateTimer -= gameTime.DeltaTime;

            if (_memoryTimer > 0)
                _memoryTimer -= gameTime.DeltaTime;

            if (_pathUpdateTimer > 0)
                _pathUpdateTimer -= gameTime.DeltaTime;
        }

        private void ChangeState(EnemyState newState)
        {
            _previousState = _currentState;
            _currentState = newState;

            switch (newState)
            {
                case EnemyState.Idle:
                    _stateTimer = _settings.IdleTime;
                    VelocityX = 0;
                    break;
                case EnemyState.Recovery:
                    _stateTimer = _settings.RecoveryTime;
                    VelocityX = 0;
                    break;
                case EnemyState.Patrolling:
                    VelocityX = _movingRight ? _settings.PatrolSpeed : -_settings.PatrolSpeed;
                    _currentPath.Clear();
                    _currentPathIndex = -1;
                    break;
                case EnemyState.Chasing:
                    if (_pathUpdateTimer <= 0)
                    {
                        UpdatePath();
                        _pathUpdateTimer = PATH_UPDATE_INTERVAL;
                    }
                    break;
            }

            Console.WriteLine($"Враг переходит в состояние: {newState}");
        }

        #region State Updates

        private void UpdatePatrolState(GameTime gameTime)
        {
            if (DetectPlayer())
            {
                ChangeState(EnemyState.Chasing);
                return;
            }

            if (_hasLastKnownPlayerPosition && _memoryTimer > 0)
            {
                Console.WriteLine("Враг помнит где был игрок и направляется туда");
                ChangeState(EnemyState.Chasing);
                return;
            }

            if (IsAtPlatformEdge())
            {
                if (NeedToJumpGap() && CanJumpGap())
                {
                    PrepareJump();
                    ChangeState(EnemyState.Jumping);
                }
                else
                {
                    _movingRight = !_movingRight;
                    VelocityX = _movingRight ? _settings.PatrolSpeed : -_settings.PatrolSpeed;
                }
                return;
            }

            if (_stateTimer <= 0 && _random.NextDouble() < _settings.IdleProbability)
            {
                ChangeState(EnemyState.Idle);
                return;
            }

            VelocityX = _movingRight ? _settings.PatrolSpeed : -_settings.PatrolSpeed;
        }

        private bool CheckDirectJumpToPlayer()
        {
            if (_player == null || !IsOnGround() || _jumpCooldown > 0)
                return false;

            Platform currentPlatform = GetPlatformUnderEnemy();
            if (currentPlatform == null)
                return false;
            if (!DetectPlayer())
                return false;
            Platform playerPlatform = GetPlatformUnderPlayer();
            if (playerPlatform == currentPlatform)
                return false;
            float dx = _player.Position.X - Position.X;
            float dy = Position.Y - _player.Position.Y; float horizontalDist = Math.Abs(dx);
            if (!IsAtCorrectEdgeForJump(dx > 0))
            {
                Console.WriteLine("ВРАГ НЕ У ПРАВИЛЬНОГО КРАЯ ПЛАТФОРМЫ ДЛЯ ПРЫЖКА!");
                return false;
            }
            if (horizontalDist > 100)
            {
                Console.WriteLine("СЛИШКОМ ДАЛЕКО ДЛЯ ПРЯМОГО ПРЫЖКА!");
                return false;
            }

            if (dy < -20 || dy > 60)
            {
                Console.WriteLine("ВЫСОТА НЕ ПОДХОДИТ ДЛЯ ПРЫЖКА!");
                return false;
            }

            if (IsGapInJumpPath(Position.X, _player.Position.X, Position.Y, _player.Position.Y))
            {
                Console.WriteLine("НА ПУТИ ПРЫЖКА ЕСТЬ ЯМА - ИСПОЛЬЗУЕМ PATHFINDER!");
                return false;
            }

            bool nearEdge = IsReallyNearPlatformEdge(dx > 0);
            if (!nearEdge)
            {
                return false;
            }

            if (HasObstaclesInJumpPath(dx, dy))
            {
                Console.WriteLine("ПРЕПЯТСТВИЕ НА ПУТИ ПРЫЖКА!");
                return false;
            }

            if (!IsPathReallySafe(dx, dy))
            {
                Console.WriteLine("ПУТЬ К ИГРОКУ НЕБЕЗОПАСЕН!");
                return false;
            }

            Console.WriteLine("РЕШЕНИЕ: ПРЫГАЕМ К ИГРОКУ НАПРЯМУЮ!");
            return true;
        }

        private bool IsReallyNearPlatformEdge(bool rightDirection)
        {
            float edgeCheckDistance = 5f;

            RectangleF edgeCheck = new RectangleF(
                Position.X + (rightDirection ? Size.Width : 0),
                Position.Y + Size.Height + 1,
                rightDirection ? edgeCheckDistance : -edgeCheckDistance,
                5
            );

            bool foundGround = false;
            foreach (var platform in _gameManager.GetPlatforms())
            {
                if (edgeCheck.IntersectsWith(platform.Bounds))
                {
                    foundGround = true;
                    break;
                }
            }

            return IsOnGround() && !foundGround;
        }

        private bool IsGapInJumpPath(float startX, float endX, float startY, float endY)
        {
            int direction = startX < endX ? 1 : -1;
            float distance = Math.Abs(endX - startX);

            int steps = Math.Max(10, (int)(distance / 10));

            bool foundGap = false;

            for (int i = 1; i < steps; i++)
            {
                float t = i / (float)steps;
                float testX = startX + distance * t * direction;

                bool hasGround = false;
                for (float yOffset = 0; yOffset <= 120; yOffset += 20)
                {
                    if (CheckGroundAt(testX, Math.Max(startY, endY) + yOffset))
                    {
                        hasGround = true;
                        break;
                    }
                }

                if (!hasGround)
                {
                    foundGap = true;
                    break;
                }
            }

            return foundGap;
        }
        private bool IsAtCorrectEdgeForJump(bool rightDirection)
        {
            Platform platform = GetPlatformUnderEnemy();
            if (platform == null)
                return false;

            float platformLeftEdge = platform.Position.X;
            float platformRightEdge = platform.Position.X + platform.Size.Width;

            if (rightDirection)
            {
                return Math.Abs(Position.X + Size.Width - platformRightEdge) < 10;
            }
            else
            {
                return Math.Abs(Position.X - platformLeftEdge) < 10;
            }
        }
        private float EstimateMaxGapWidth(int direction)
        {
            float startX = direction > 0 ? Position.X + Size.Width : Position.X;
            float startY = Position.Y + Size.Height;

            const float CHECK_STEP = 5f;
            const float MAX_DISTANCE = 300f;

            float maxGapWidth = 0;
            float currentGapWidth = 0;
            bool inGap = false;

            for (float dist = 0; dist <= MAX_DISTANCE; dist += CHECK_STEP)
            {
                float testX = startX + dist * direction;

                bool hasGround = CheckGroundAt(testX, startY + 5);

                if (!hasGround && !inGap)
                {
                    inGap = true;
                    currentGapWidth = 0;
                }
                else if (!hasGround && inGap)
                {
                    currentGapWidth += CHECK_STEP;
                }
                else if (hasGround && inGap)
                {
                    inGap = false;
                    maxGapWidth = Math.Max(maxGapWidth, currentGapWidth);
                }
            }

            if (inGap)
            {
                maxGapWidth = Math.Max(maxGapWidth, currentGapWidth);
            }

            return maxGapWidth;
        }

        private bool IsPathReallySafe(float dx, float dy)
        {
            float targetX = Position.X + dx;
            float targetY = Position.Y - dy;

            bool hasLandingPlatform = false;

            for (float xOffset = -10; xOffset <= 10; xOffset += 5)
            {
                if (CheckGroundAt(targetX + xOffset, targetY + Size.Height + 2))
                {
                    hasLandingPlatform = true;
                    break;
                }
            }

            if (!hasLandingPlatform)
            {
                Console.WriteLine("НЕТ НАДЕЖНОЙ ПЛАТФОРМЫ ДЛЯ ПРИЗЕМЛЕНИЯ!");
                return false;
            }

            float distFromEdge = 0;
            int direction = dx > 0 ? 1 : -1;

            for (float offset = 0; offset < 40; offset += 5)
            {
                float checkX = targetX + direction * offset;
                if (!CheckGroundAt(checkX, targetY + Size.Height + 2))
                {
                    distFromEdge = offset;
                    break;
                }
            }

            if (distFromEdge < 15)
            {
                Console.WriteLine("МЕСТО ПРИЗЕМЛЕНИЯ СЛИШКОМ БЛИЗКО К КРАЮ!");
                return false;
            }

            return true;
        }

        private bool IsNearPlatformEdge(bool rightDirection)
        {
            float edgeCheckDistance = rightDirection ?
                Size.Width + 5 : -5;
            RectangleF edgeCheck = new RectangleF(
                Position.X + (rightDirection ? Size.Width : 0),
                Position.Y + Size.Height,
                rightDirection ? 10 : -10,
                5
            );

            bool foundGround = false;
            foreach (var platform in _gameManager.GetPlatforms())
            {
                if (edgeCheck.IntersectsWith(platform.Bounds))
                {
                    foundGround = true;
                    break;
                }
            }

            if (foundGround)
            {
                float distance = 0;
                const float MAX_CHECK = 40f; const float CHECK_STEP = 5f;

                for (float offset = 0; offset <= MAX_CHECK; offset += CHECK_STEP)
                {
                    float checkX = Position.X + (rightDirection ?
                        Size.Width + offset :
                        -offset);

                    RectangleF check = new RectangleF(
                        checkX - 2,
                        Position.Y + Size.Height,
                        4,
                        5
                    );

                    bool hasGround = false;
                    foreach (var platform in _gameManager.GetPlatforms())
                    {
                        if (check.IntersectsWith(platform.Bounds))
                        {
                            hasGround = true;
                            break;
                        }
                    }

                    if (!hasGround)
                    {
                        distance = offset;
                        break;
                    }
                }

                return distance > 0 && distance < 20;
            }

            return true;
        }

        private Platform GetPlatformUnderEnemy()
        {
            RectangleF checkRect = new RectangleF(
                Position.X,
                Position.Y + Size.Height,
                Size.Width,
                5
            );

            foreach (var platform in _gameManager.GetPlatforms())
            {
                if (checkRect.IntersectsWith(platform.Bounds))
                    return platform;
            }

            return null;
        }

        private Platform GetPlatformUnderPlayer()
        {
            if (_player == null)
                return null;

            RectangleF checkRect = new RectangleF(
                _player.Position.X,
                _player.Position.Y + _player.Size.Height,
                _player.Size.Width,
                5
            );

            foreach (var platform in _gameManager.GetPlatforms())
            {
                if (checkRect.IntersectsWith(platform.Bounds))
                    return platform;
            }

            return null;
        }

        private bool HasObstaclesInJumpPath(float dx, float dy)
        {
            int steps = 12;
            float landingX = Position.X + dx;

            bool hasSafeLanding = false;

            for (float xOffset = -15; xOffset <= 15; xOffset += 5)
            {
                if (CheckGroundAt(landingX + xOffset, Position.Y - dy + Size.Height + 5))
                {
                    hasSafeLanding = true;
                    break;
                }
            }

            if (!hasSafeLanding)
            {
                Console.WriteLine("НЕТ БЕЗОПАСНОГО МЕСТА ПРИЗЕМЛЕНИЯ!");
                return true;
            }

            float jumpHeight = Math.Max(40, dy + 30);
            int direction = dx > 0 ? 1 : -1;
            float distance = Math.Abs(dx);

            for (int i = 1; i < steps; i++)
            {
                float t = i / (float)steps;

                float x = Position.X + dx * t;
                float y = Position.Y - jumpHeight * 4.5f * t * (1 - t);
                if (dy > 0)
                {
                    y -= dy * t;
                }
                else if (dy < 0 && t > 0.5f)
                {
                    float descent = dy * (t - 0.5f) * 2; y -= descent;
                }

                RectangleF checkRect = new RectangleF(x - Size.Width / 2, y - Size.Height / 2, Size.Width, Size.Height);

                foreach (var platform in _gameManager.GetPlatforms())
                {
                    if (platform.Size.Height > 5 && checkRect.IntersectsWith(platform.Bounds))
                    {
                        if (i <= 2 || i >= steps - 2)
                            continue;

                        if (t < 0.5f && platform.Position.Y > Position.Y + Size.Height / 2)
                            continue;

                        float targetY = Position.Y - dy;
                        if (t > 0.5f && platform.Position.Y + platform.Size.Height < targetY)
                            continue;

                        return true;
                    }
                }
            }

            return false;
        }

        private bool CheckGroundAt(float x, float y)
        {
            RectangleF checkRect = new RectangleF(x - 5, y, 10, 5);

            foreach (var platform in _gameManager.GetPlatforms())
            {
                if (checkRect.IntersectsWith(platform.Bounds))
                    return true;
            }

            return false;
        }

        private void DoDirectJumpToPlayer()
        {
            float dx = _player.Position.X - Position.X;
            float dy = Position.Y - _player.Position.Y;
            float horizontalDist = Math.Abs(dx);

            float jumpStrength = _settings.JumpStrength;

            float speedBoost = _settings.JumpSpeedBoost;

            if (horizontalDist < 60)
            {
                jumpStrength *= 0.85f;
                speedBoost *= 0.9f;
            }
            else if (horizontalDist < 80)
            {
                jumpStrength *= 0.9f;
                speedBoost *= 0.95f;
            }

            if (dy > 20)
            {
                jumpStrength *= 1.05f + (dy / 200f);
            }

            if (jumpStrength > _settings.JumpStrength * 1.2f)
                jumpStrength = _settings.JumpStrength * 1.2f;

            if (speedBoost > _settings.JumpSpeedBoost * 1.2f)
                speedBoost = _settings.JumpSpeedBoost * 1.2f;

            VelocityY = -jumpStrength;

            _movingRight = dx > 0;
            VelocityX = _movingRight ?
                _settings.ChaseSpeed * speedBoost :
                -_settings.ChaseSpeed * speedBoost;


            Console.WriteLine($"ПРЫЖОК К ИГРОКУ: сила={jumpStrength}, скорость={Math.Abs(VelocityX)}, дистанция={horizontalDist}");
        }

        private bool IsPathSafe(float dx, float dy)
        {
            float targetX = Position.X + dx;
            float targetY = Position.Y - dy;

            bool hasLandingPlatform = false;

            for (float xOffset = -20; xOffset <= 20; xOffset += 10)
            {
                if (CheckGroundAt(targetX + xOffset, targetY + Size.Height + 5))
                {
                    hasLandingPlatform = true;
                    break;
                }
            }

            if (!hasLandingPlatform)
            {
                return false;
            }

            if (Math.Abs(dx) > 150)
            {
                float distance = Math.Abs(dx);
                int direction = dx > 0 ? 1 : -1;

                float jumpHeight = Math.Max(40, dy + 30);

                for (float t = 0.3f; t <= 0.7f; t += 0.2f)
                {
                    float x = Position.X + dx * t;
                    float y = Position.Y - jumpHeight * 4 * t * (1 - t);

                    if (dy > 0)
                    {
                        y -= dy * t;
                    }

                    RectangleF checkRect = new RectangleF(x - Size.Width / 2, y - Size.Height / 2, Size.Width, Size.Height);

                    foreach (var platform in _gameManager.GetPlatforms())
                    {
                        if (platform.Size.Height > 5 && checkRect.IntersectsWith(platform.Bounds))
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        private bool CanJumpGapWidth(float width)
        {
            float maxJumpDistance = _settings.ChaseSpeed * _settings.JumpSpeedBoost * 12f;

            if (width > maxJumpDistance * 0.85f)
            {
                return false;
            }

            return width < maxJumpDistance;
        }

        private void UpdateChaseState(GameTime gameTime)
        {

            bool canSeePlayer = DetectPlayer();

            if (canSeePlayer)
            {
                _hasLastKnownPlayerPosition = true;
                _lastKnownPlayerPosition = _player.Position;
                _memoryTimer = _settings.MemoryDuration;
            }
            else if (!_hasLastKnownPlayerPosition || _memoryTimer <= 0)
            {
                _currentPath.Clear();
                _currentPathIndex = -1;
                ChangeState(EnemyState.Patrolling);
                return;
            }

            if (canSeePlayer && IsOnGround() && _jumpCooldown <= 0)
            {
                float dx = _player.Position.X - Position.X;
                bool shouldMoveRight = dx > 0;

                if (!IsAtCorrectEdgeForJump(shouldMoveRight))
                {
                    _movingRight = shouldMoveRight;
                    VelocityX = _movingRight ? _settings.ChaseSpeed : -_settings.ChaseSpeed;
                    return;
                }

                if (CheckDirectJumpToPlayer())
                {
                    DoDirectJumpToPlayer();
                    return;
                }
            }

            if (_pathUpdateTimer <= 0)
            {
                UpdatePath();
                _pathUpdateTimer = PATH_UPDATE_INTERVAL;
            }

            if (IsOnGround() && NeedToJumpGap())
            {
                if (CanJumpGap())
                {
                    PrepareJump();
                    ChangeState(EnemyState.Jumping);
                }
                else
                {
                    UpdatePath();
                    _movingRight = !_movingRight;
                }
                return;
            }

            if (IsOnGround() && _jumpCooldown <= 0)
            {
                if (_currentPathIndex >= 0 && _currentPathIndex < _currentPath.Count)
                {
                    PointF nextPoint = _currentPath[_currentPathIndex];
                    if (Position.Y - nextPoint.Y > 20 && Math.Abs(nextPoint.X - Position.X) < 150)
                    {
                        _movingRight = nextPoint.X > Position.X;
                        Jump();
                        return;
                    }
                }
                else if (canSeePlayer && ShouldJumpToReachPlayer())
                {
                    Jump();
                    return;
                }
                else if (!canSeePlayer && _hasLastKnownPlayerPosition && ShouldJumpToReachPoint(_lastKnownPlayerPosition))
                {
                    Jump();
                    return;
                }
            }

            if (_currentPath.Count > 0 && _currentPathIndex >= 0 && _currentPathIndex < _currentPath.Count)
            {
                FollowPath();
            }
            else if (canSeePlayer)
            {
                MoveTowardsPlayer();
            }
            else
            {
                MoveTowardsPoint(_lastKnownPlayerPosition);
            }
        }

        private void UpdateJumpState(GameTime gameTime)
        {

            if (IsOnGround() && VelocityY >= 0)
            {
                ChangeState(EnemyState.Recovery);
                return;
            }

            VelocityX = _movingRight ?
    _settings.ChaseSpeed * _settings.JumpSpeedBoost :
    -_settings.ChaseSpeed * _settings.JumpSpeedBoost;
        }

        private void UpdateRecoveryState(GameTime gameTime)
        {
            if (_stateTimer <= 0)
            {
                ChangeState(DetectPlayer() ? EnemyState.Chasing : EnemyState.Patrolling);
            }
        }

        private void UpdateIdleState(GameTime gameTime)
        {
            _breathingOffset = (int)(Math.Sin(_stateTimer * 5) * 2);

            if (_stateTimer <= 0)
            {
                _breathingOffset = 0;
                ChangeState(DetectPlayer() ? EnemyState.Chasing : EnemyState.Patrolling);
            }
        }

        #endregion

        #region Path Finding and Movement

        private void FollowPath()
        {
            if (_currentPathIndex < 0 || _currentPathIndex >= _currentPath.Count)
                return;

            PointF target = _currentPath[_currentPathIndex];

            float distanceToTarget = Math.Abs(target.X - Position.X);
            if (distanceToTarget < 15)
            {
                _currentPathIndex++;

                if (_currentPathIndex >= _currentPath.Count)
                {
                    _currentPath.Clear();
                    _currentPathIndex = -1;
                    return;
                }

                target = _currentPath[_currentPathIndex];
            }

            if (Position.Y - target.Y > 20 && IsOnGround())
            {
                if (IsGapInJumpPath(Position.X, target.X, Position.Y, target.Y))
                {
                    if (target.X > Position.X + 5)
                    {
                        _movingRight = true;
                        VelocityX = _settings.ChaseSpeed;
                    }
                    else if (target.X < Position.X - 5)
                    {
                        _movingRight = false;
                        VelocityX = -_settings.ChaseSpeed;
                    }
                    return;
                }
            }

            if (target.X > Position.X + 5)
            {
                _movingRight = true;
                VelocityX = _settings.ChaseSpeed;
            }
            else if (target.X < Position.X - 5)
            {
                _movingRight = false;
                VelocityX = -_settings.ChaseSpeed;
            }
            else
            {
                VelocityX = 0;
            }
        }

        private void UpdatePath()
        {
            PointF targetPosition = _player != null && DetectPlayer()
                ? _player.Position
                : (_hasLastKnownPlayerPosition ? _lastKnownPlayerPosition : Position);

            _currentPath = _pathFinder.FindPath(
    Position,
    targetPosition,
    Size.Width,
    Size.Height
);

            _currentPathIndex = _currentPath.Count > 0 ? 0 : -1;

            if (_currentPath.Count > 0)
            {
                Console.WriteLine($"Найден путь с {_currentPath.Count} точками");
            }
            else
            {
                Console.WriteLine("Путь не найден, движение напрямую");
            }
        }

        private bool DetectPlayer()
        {
            if (_player == null) return false;

            float distanceX = Math.Abs(_player.Position.X - Position.X);
            float distanceY = Math.Abs(_player.Position.Y - Position.Y);

            float effectiveRange = _currentState == EnemyState.Chasing ?
    _settings.DetectionRange * 1.3f : _settings.DetectionRange;

            if (distanceX > effectiveRange || distanceY > effectiveRange * 0.75f)
                return false;

            return CheckLineOfSightImproved(_player.Position);
        }

        private bool CheckLineOfSightImproved(PointF targetPos)
        {
            PointF start = new PointF(Position.X + Size.Width / 2, Position.Y + Size.Height / 2);
            PointF end = new PointF(targetPos.X + _player.Size.Width / 2, targetPos.Y + _player.Size.Height / 2);

            float dx = end.X - start.X;
            float dy = end.Y - start.Y;
            float distance = (float)Math.Sqrt(dx * dx + dy * dy);

            if (distance < 0.001f) return true;

            float nx = dx / distance;
            float ny = dy / distance;

            int stepsCount = distance < 150 ? 4 : 8;

            for (int i = 1; i < stepsCount; i++)
            {
                float t = i / (float)stepsCount;
                PointF checkPoint = new PointF(
                    start.X + nx * distance * t,
                    start.Y + ny * distance * t
                );

                RectangleF checkRect = new RectangleF(checkPoint.X - 3, checkPoint.Y - 3, 6, 6);

                bool blocked = false;
                foreach (var platform in _gameManager.GetPlatforms())
                {
                    if (platform.Size.Height > 15 && checkRect.IntersectsWith(platform.Bounds))
                    {
                        blocked = true;
                        break;
                    }
                }

                if (blocked)
                {
                    RectangleF playerFeetCheck = new RectangleF(
    end.X - 5,
    targetPos.Y + _player.Size.Height - 5,
    10, 10);

                    RectangleF playerHeadCheck = new RectangleF(
                        end.X - 5,
                        targetPos.Y + 5,
                        10, 10);

                    if (!IsPathBlocked(start, playerFeetCheck.Location) ||
    !IsPathBlocked(start, playerHeadCheck.Location))
                    {
                        return true;
                    }

                    return false;
                }
            }

            if (Math.Abs(ny) < 0.3f && Math.Abs(dx) > 30f)
            {
                if (IsGapBetweenPoints(start, end))
                {
                    if (distance < 150)
                        return true;

                    return false;
                }
            }

            return true;
        }

        private bool IsPathBlocked(PointF start, PointF end)
        {
            float dx = end.X - start.X;
            float dy = end.Y - start.Y;
            float distance = (float)Math.Sqrt(dx * dx + dy * dy);

            if (distance < 0.001f) return false;

            float nx = dx / distance;
            float ny = dy / distance;

            int steps = 5;
            for (int i = 1; i < steps; i++)
            {
                float t = i / (float)steps;
                PointF checkPoint = new PointF(
                    start.X + nx * distance * t,
                    start.Y + ny * distance * t
                );

                RectangleF checkRect = new RectangleF(checkPoint.X - 2, checkPoint.Y - 2, 4, 4);

                foreach (var platform in _gameManager.GetPlatforms())
                {
                    if (platform.Size.Height > 15 && checkRect.IntersectsWith(platform.Bounds))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool IsGapBetweenPoints(PointF start, PointF end)
        {
            int direction = start.X < end.X ? 1 : -1;
            float distance = Math.Abs(end.X - start.X);
            int steps = Math.Max(5, (int)(distance / 20));

            for (int i = 1; i < steps; i++)
            {
                float testX = start.X + direction * (distance * i / steps);

                RectangleF groundCheck = new RectangleF(
    testX - 2,
    Math.Min(start.Y, end.Y),
    4,
    Math.Abs(start.Y - end.Y) + 100
);

                bool foundGround = false;
                foreach (var platform in _gameManager.GetPlatforms())
                {
                    if (groundCheck.IntersectsWith(platform.Bounds))
                    {
                        foundGround = true;
                        break;
                    }
                }

                if (!foundGround)
                    return true;
            }

            return false;
        }

        private void MoveTowardsPlayer()
        {
            if (_player == null) return;

            if (_player.Position.X > Position.X + 5)
            {
                _movingRight = true;
                VelocityX = _settings.ChaseSpeed;
            }
            else if (_player.Position.X < Position.X - 5)
            {
                _movingRight = false;
                VelocityX = -_settings.ChaseSpeed;
            }
            else
            {
                VelocityX = 0;
            }
        }

        private void MoveTowardsPoint(PointF target)
        {
            if (target.X > Position.X + 5)
            {
                _movingRight = true;
                VelocityX = _settings.ChaseSpeed;
            }
            else if (target.X < Position.X - 5)
            {
                _movingRight = false;
                VelocityX = -_settings.ChaseSpeed;
            }
            else
            {
                VelocityX = 0;

                if (DetectPlayer())
                {
                    _hasLastKnownPlayerPosition = true;
                    _lastKnownPlayerPosition = _player.Position;
                    _memoryTimer = _settings.MemoryDuration;
                }
                else
                {
                    _hasLastKnownPlayerPosition = false;
                }
            }
        }

        private bool ShouldJumpToReachPoint(PointF target)
        {
            if (!IsOnGround() || _jumpCooldown > 0)
                return false;

            float verticalDiff = Position.Y - target.Y;
            float horizontalDist = Math.Abs(target.X - Position.X);

            return verticalDiff > 30 && horizontalDist < 120;
        }

        #endregion

        #region Jumping and Gap Handling

        private bool NeedToJumpGap()
        {
            if (!IsOnGround())
                return false;

            int direction = _movingRight ? 1 : -1;
            float checkDistance = 20f;

            RectangleF checkRect = new RectangleF(
    Position.X + (direction * (Size.Width / 2 + checkDistance)),
    Position.Y + Size.Height + 1,
    10,
    20
);

            bool foundGround = false;
            foreach (var platform in _gameManager.GetPlatforms())
            {
                if (checkRect.IntersectsWith(platform.Bounds))
                {
                    foundGround = true;
                    break;
                }
            }

            if (!foundGround)
            {
                _gapWidth = MeasureGapWidth(direction);
                return true;
            }

            return false;
        }

        private float MeasureGapWidth(int direction)
        {
            float startX = direction > 0 ?
                Position.X + Size.Width + 2 :
                Position.X - 2;
            float startY = Position.Y + Size.Height;

            const float CHECK_STEP = 5f;
            const float MAX_CHECK_DISTANCE = 300f;

            float gapStart = startX;
            float gapEnd = startX;
            bool foundGap = false;
            bool foundEnd = false;

            for (float testX = startX;
                 Math.Abs(testX - startX) <= MAX_CHECK_DISTANCE;
                 testX += direction * CHECK_STEP)
            {
                bool hasGround = false;

                RectangleF testRect = new RectangleF(testX - 2, startY, 4, 30);

                foreach (var platform in _gameManager.GetPlatforms())
                {
                    if (testRect.IntersectsWith(platform.Bounds))
                    {
                        hasGround = true;
                        break;
                    }
                }

                if (!hasGround && !foundGap)
                {
                    foundGap = true;
                    gapStart = testX;
                }
                else if (hasGround && foundGap && !foundEnd)
                {
                    foundEnd = true;
                    gapEnd = testX;
                    break;
                }
            }

            if (foundGap && foundEnd)
            {
                return Math.Abs(gapEnd - gapStart);
            }

            return MAX_CHECK_DISTANCE;
        }

        private bool CanJumpGap()
        {
            float maxJumpDistance = _settings.ChaseSpeed * _settings.JumpSpeedBoost * 14f;

            if (_currentState == EnemyState.Chasing)
                maxJumpDistance *= 1.2f;

            return _gapWidth < maxJumpDistance;
        }

        private void PrepareJump()
        {
            float jumpStrength = _settings.JumpStrength * 1.05f;

            float speedBoost = _settings.JumpSpeedBoost * 1.2f;

            if (_gapWidth > 150f)
            {
                jumpStrength *= 1.2f; speedBoost *= 1.3f;
            }
            else if (_gapWidth > 100f)
            {
                jumpStrength *= 1.1f; speedBoost *= 1.2f;
            }
            else if (_gapWidth > 50f)
            {
                jumpStrength *= 1.05f; speedBoost *= 1.1f;
            }

            VelocityX = _movingRight ? _settings.ChaseSpeed * speedBoost : -_settings.ChaseSpeed * speedBoost;
            VelocityY = -jumpStrength;


            Console.WriteLine($"Прыжок через пропасть шириной {_gapWidth}. Сила={jumpStrength}, Скорость={Math.Abs(VelocityX)}");
        }

        private bool ShouldJumpToReachPlayer()
        {
            if (_player == null || !IsOnGround() || _jumpCooldown > 0)
                return false;

            float verticalDiff = Position.Y - _player.Position.Y;
            float horizontalDist = Math.Abs(_player.Position.X - Position.X);

            if (verticalDiff <= 20 || horizontalDist >= 120 || horizontalDist < 30)
                return false;
            Platform enemyPlatform = GetPlatformUnderEnemy();
            Platform playerPlatform = GetPlatformUnderPlayer();
            if (enemyPlatform == playerPlatform)
                return false;
            if (playerPlatform == null)
                return false;
            if (IsGapInJumpPath(Position.X, _player.Position.X, Position.Y, _player.Position.Y))
                return false;
            float dx = _player.Position.X - Position.X;
            float dy = Position.Y - _player.Position.Y;
            if (HasObstaclesInJumpPath(dx, dy))
                return false;


            Console.WriteLine("РЕШЕНИЕ: ПРЫГАЕМ ВВЕРХ К ИГРОКУ!");
            return true;
        }

        private void Jump()
        {
            VelocityY = -_settings.JumpStrength;
            _jumpCooldown = _settings.JumpCooldown;

            if (_player != null)
            {
                _movingRight = _player.Position.X > Position.X;
            }

            Console.WriteLine("Прыжок вверх для достижения игрока!");
        }

        private bool IsAtPlatformEdge()
        {
            int direction = _movingRight ? 1 : -1;
            float edgeCheckDistance = 10f;

            RectangleF edgeCheck = new RectangleF(
                Position.X + (direction * (Size.Width / 2)),
                Position.Y + Size.Height + 1,
                edgeCheckDistance * direction,
                5
            );

            bool foundGround = false;
            foreach (var platform in _gameManager.GetPlatforms())
            {
                if (edgeCheck.IntersectsWith(platform.Bounds))
                {
                    foundGround = true;
                    break;
                }
            }

            return !foundGround && IsOnGround();
        }

        #endregion

        #region Utility Methods

        private void ApplyGravity()
        {
            float gravityFactor = 0.55f;

            if (_currentState == EnemyState.Jumping)
            {
                if (VelocityY < 0)
                    gravityFactor = 0.45f;
                else
                    gravityFactor = 0.6f;
            }

            VelocityY += gravityFactor;

            if (VelocityY > 12f)
                VelocityY = 12f;
        }

        private void CheckWorldBounds()
        {
            if (Position.X < 0)
            {
                Position = new PointF(0, Position.Y);
                _movingRight = true;
            }
            else if (Position.X + Size.Width > 3000)
            {
                Position = new PointF(3000 - Size.Width, Position.Y);
                _movingRight = false;
            }
        }

        protected override bool IsOnGround()
        {
            RectangleF testRect = new RectangleF(
    Position.X + 2,
    Position.Y + Size.Height + 1,
    Size.Width - 4,
    2
);

            foreach (var platform in _gameManager.GetPlatforms())
            {
                if (testRect.IntersectsWith(platform.Bounds))
                    return true;
            }

            return false;
        }

        public override void Draw(Camera camera)
        {
            if (Sprite != null && !Sprite.IsDisposed)
            {
                if (camera.IsInView(Bounds))
                {
                    PointF screenPos = camera.WorldToScreen(Position);

                    switch (_currentState)
                    {
                        case EnemyState.Idle:
                            Sprite.Height = 32 + _breathingOffset;
                            Sprite.Location = new Point(
                                (int)screenPos.X,
                                (int)screenPos.Y - _breathingOffset / 2);
                            break;

                        case EnemyState.Jumping:
                            if (VelocityY < 0)
                            {
                                Sprite.Height = 36;
                                Sprite.Location = new Point((int)screenPos.X, (int)screenPos.Y - 4);
                            }
                            else
                            {
                                Sprite.Height = 34;
                                Sprite.Location = new Point((int)screenPos.X, (int)screenPos.Y - 2);
                            }
                            break;

                        default:
                            Sprite.Height = 32;
                            Sprite.Location = new Point((int)screenPos.X, (int)screenPos.Y);
                            break;
                    }

                    Sprite.Visible = true;

                }
                else
                {
                    Sprite.Visible = false;
                }
            }
        }

        private void DrawDebugPath(Camera camera)
        {
            if (_currentPath.Count <= 0 || _currentPathIndex < 0)
                return;

        }

        public override void Reset(Camera camera)
        {
            base.Reset(camera);

            _currentState = EnemyState.Patrolling;
            _stateTimer = 0;
            _jumpCooldown = 0;
            _breathingOffset = 0;
            _hasLastKnownPlayerPosition = false;
            _memoryTimer = 0;

            _currentPath.Clear();
            _currentPathIndex = -1;
            _pathUpdateTimer = 0;
        }

        #endregion
    }


    public class FlyingEnemy : Enemy
    {
        private readonly float _detectionRange;
        private readonly float _flyHeight;
        private bool _isChasing;
        private bool _movingUp;

        public FlyingEnemy(float x, float y, Player player, float speed, float detectionRange, float flyHeight, Image image)
            : base(x, y, 32, 32, player, speed, image)
        {
            _detectionRange = detectionRange;
            _flyHeight = flyHeight;
            _movingUp = true;

            if (image == null && Sprite != null)
            {
                Sprite.BackColor = Color.MediumPurple;
            }
        }

        public override void Update(GameTime gameTime, Camera camera)
        {
            _isChasing = DetectPlayer(_detectionRange);

            if (_isChasing && _player != null)
            {
                float dx = _player.Position.X - Position.X;
                float dy = _player.Position.Y - Position.Y;

                float length = (float)Math.Sqrt(dx * dx + dy * dy);
                if (length > 0)
                {
                    dx /= length;
                    dy /= length;
                }

                VelocityX = dx * _speed;
                VelocityY = dy * _speed;
            }
            else
            {
                VelocityX = 0;

                if (_movingUp)
                {
                    VelocityY = -_speed * 0.5f;

                    if (Position.Y <= _initialPosition.Y - _flyHeight)
                    {
                        _movingUp = false;
                    }
                }
                else
                {
                    VelocityY = _speed * 0.5f;
                    if (Position.Y >= _initialPosition.Y)
                    {
                        _movingUp = true;
                    }
                }
            }

            Position = new PointF(Position.X + VelocityX, Position.Y + VelocityY);

            Draw(camera);
        }

        protected override void HandlePlatformCollision(Platform platform)
        {
            if (_isChasing)
            {
                if (VelocityX > 0 && Position.X + Size.Width > platform.Position.X &&
                    Position.X < platform.Position.X)
                {
                    Position = new PointF(platform.Position.X - Size.Width, Position.Y);
                    VelocityX = -VelocityX;
                }
                else if (VelocityX < 0 && Position.X < platform.Position.X + platform.Size.Width &&
                        Position.X + Size.Width > platform.Position.X + platform.Size.Width)
                {
                    Position = new PointF(platform.Position.X + platform.Size.Width, Position.Y);
                    VelocityX = -VelocityX;
                }

                if (VelocityY > 0 && Position.Y + Size.Height > platform.Position.Y &&
                    Position.Y < platform.Position.Y)
                {
                    Position = new PointF(Position.X, platform.Position.Y - Size.Height);
                    VelocityY = -VelocityY;
                }
                else if (VelocityY < 0 && Position.Y < platform.Position.Y + platform.Size.Height &&
                        Position.Y + Size.Height > platform.Position.Y + platform.Size.Height)
                {
                    Position = new PointF(Position.X, platform.Position.Y + platform.Size.Height);
                    VelocityY = -VelocityY;
                }
            }
        }
    }
}