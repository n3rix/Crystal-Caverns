using System;
using System.Collections.Generic;
using System.Drawing;
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

    public class ChasingEnemy : Enemy
    {
        private readonly float _detectionRange;
        private bool _isChasing;
        private bool _movingRight;
        private List<PointF> _currentPath = new List<PointF>();
        private int _currentPathIndex = -1;
        private float _pathUpdateTimer = 0;
        private const float PATH_UPDATE_INTERVAL = 1.0f;
        private NavigationGraph _navigationGraph;
        private bool _shouldJump = false;
        private float _jumpCooldown = 0;
        private const float JUMP_COOLDOWN_TIME = 0.5f;

        private float _changeDirectionTimer = 0;
        private float _idleTimer = 0;
        private bool _isIdle = false;
        private const float MIN_DIRECTION_CHANGE_TIME = 0.5f;
        private const float IDLE_TIME = 1.0f;
        private const float CHASE_HYSTERESIS = 50.0f;

        private bool _needToJumpOverGap = false;
        private float _jumpDistance = 0f;
        private float _minJumpSpeed = 5.0f;

        private const float JUMP_FORCE_BASE = 8.0f; private const float JUMP_FORCE_MAX = 10.0f; private const float PRE_JUMP_ACCELERATION = 1.0f; private float _preJumpTimer = 0f; private const float PRE_JUMP_TIME = 0.2f;
        private bool _isInJumpOverGapMode = false; private float _jumpBoostFactor = 1f;
        private float _gapJumpTimer = 0f;

        private GameManager _gameManager;
        private int _breathingOffset = 0;

        public ChasingEnemy(float x, float y, Player player, float speed, float detectionRange, Image image)
            : base(x, y, 32, 32, player, speed, image)
        {
            _detectionRange = detectionRange;
            _movingRight = true;
            _gameManager = GameManager.Instance;

            if (image == null && Sprite != null)
            {
                Sprite.BackColor = Color.Crimson;
            }

            _navigationGraph = new NavigationGraph(_gameManager.GetPlatforms());
        }

        public override void Update(GameTime gameTime, Camera camera)
        {
            _changeDirectionTimer -= gameTime.DeltaTime;
            _pathUpdateTimer -= gameTime.DeltaTime;
            if (_jumpCooldown > 0)
                _jumpCooldown -= gameTime.DeltaTime;
            if (_gapJumpTimer > 0)
            {
                _gapJumpTimer -= gameTime.DeltaTime;
            }
            if (IsOnGround() && !_needToJumpOverGap && !_isInJumpOverGapMode)
            {
                int direction = _player.Position.X > Position.X ? 1 : -1;
                bool isGapAhead = CheckForGapAhead(direction, out float gapWidth);

                if (isGapAhead)
                {
                    _needToJumpOverGap = true;
                    _jumpDistance = gapWidth;
                    _preJumpTimer = PRE_JUMP_TIME;
                    _movingRight = direction > 0;
                    VelocityX = _movingRight ? Math.Max(_speed, _minJumpSpeed) : Math.Min(-_speed, -_minJumpSpeed);
                    Console.WriteLine($"ПРИОРИТЕТНАЯ ПРОВЕРКА: Обнаружена пропасть шириной {gapWidth}! Готовимся к прыжку.");
                }
            }
            if (_preJumpTimer > 0)
            {
                _preJumpTimer -= gameTime.DeltaTime;
                VelocityX += _movingRight ? PRE_JUMP_ACCELERATION : -PRE_JUMP_ACCELERATION;

                if (_movingRight && VelocityX > _speed * 1.5f)
                    VelocityX = _speed * 1.5f;
                else if (!_movingRight && VelocityX < -_speed * 1.5f)
                    VelocityX = -_speed * 1.5f;

                if (_preJumpTimer <= 0 && IsOnGround() && _jumpCooldown <= 0 && _needToJumpOverGap)
                {
                    float jumpForceBase = JUMP_FORCE_BASE;
                    float jumpForceExtra = 0;

                    if (_jumpDistance < 50f)
                    {
                        jumpForceBase = JUMP_FORCE_BASE * 0.8f;
                        _jumpBoostFactor = 1.8f;
                    }
                    else if (_jumpDistance < 100f)
                    {
                        jumpForceBase = JUMP_FORCE_BASE * 1.0f;
                        jumpForceExtra = (_jumpDistance - 50f) / 50f; _jumpBoostFactor = 2.0f;
                    }
                    else
                    {
                        jumpForceBase = JUMP_FORCE_BASE * 1.2f;
                        jumpForceExtra = (_jumpDistance - 100f) / 100f;
                        jumpForceExtra = Math.Min(jumpForceExtra, 1.0f);
                        _jumpBoostFactor = 2.2f + (jumpForceExtra * 0.5f);
                    }

                    float jumpForce = jumpForceBase + jumpForceExtra;
                    jumpForce = Math.Min(jumpForce, JUMP_FORCE_MAX);

                    Position = new PointF(Position.X + (_movingRight ? 3f : -3f), Position.Y);

                    VelocityY = -jumpForce;
                    VelocityX = _movingRight ? _speed * _jumpBoostFactor : -_speed * _jumpBoostFactor;

                    _isInJumpOverGapMode = true;
                    _jumpCooldown = JUMP_COOLDOWN_TIME;
                    _needToJumpOverGap = false;

                    _gapJumpTimer = 0.5f + (_jumpDistance / 100f);
                }
            }

            if (_isIdle)
            {
                _idleTimer -= gameTime.DeltaTime;
                if (_idleTimer <= 0)
                {
                    _isIdle = false;
                }

                if (Sprite != null)
                {
                    int originalHeight = 32;
                    int breathingAmplitude = 2;
                    int breathingOffset = (int)(Math.Sin(_idleTimer * 5) * breathingAmplitude);
                    _breathingOffset = breathingOffset;
                }

                VelocityX = 0;
            }
            else
            {
                _breathingOffset = 0;

                bool playerInRange = DetectPlayerWithHysteresis(_detectionRange, CHASE_HYSTERESIS);

                if (playerInRange != _isChasing && _changeDirectionTimer <= 0)
                {
                    _isChasing = playerInRange;
                    _changeDirectionTimer = MIN_DIRECTION_CHANGE_TIME;

                    if (_isChasing)
                    {
                        _pathUpdateTimer = 0;
                    }
                }

                if (_isChasing && _player != null)
                {
                    bool directMovePossible = MoveDirectlyTowardsPlayer();
                    if (ShouldJumpToReachPlayer() && _jumpCooldown <= 0 && IsOnGround())
                    {
                        VelocityY = -10f;
                        _jumpCooldown = JUMP_COOLDOWN_TIME;

                        if (_player.Position.X < Position.X)
                        {
                            VelocityX = -_speed;
                            _movingRight = false;
                        }
                        else
                        {
                            VelocityX = _speed;
                            _movingRight = true;
                        }

                        Console.WriteLine("Прыжок вверх для достижения игрока на платформе выше!");
                    }
                    if (!directMovePossible)
                    {
                        if (_pathUpdateTimer <= 0)
                        {
                            _currentPath = _navigationGraph.FindPath(Position, _player.Position);
                            _currentPathIndex = _currentPath.Count > 1 ? 1 : -1;
                            _pathUpdateTimer = PATH_UPDATE_INTERVAL;
                        }

                        if (_preJumpTimer <= 0 && !_isInJumpOverGapMode)
                        {
                            if (IsOnGround() && !_needToJumpOverGap)
                            {
                                bool isGapAhead = CheckForGapAhead(_movingRight ? 1 : -1, out float gapWidth);
                                if (isGapAhead)
                                {
                                    _needToJumpOverGap = true;
                                    _jumpDistance = gapWidth;
                                    _preJumpTimer = PRE_JUMP_TIME;
                                    VelocityX = _movingRight ? Math.Max(_speed, _minJumpSpeed) : Math.Min(-_speed, -_minJumpSpeed);
                                    Console.WriteLine($"Обнаружена пропасть шириной {gapWidth}! Готовимся к прыжку.");
                                }
                            }
                            else
                            {
                                if (_currentPathIndex >= 0 && _currentPathIndex < _currentPath.Count)
                                {
                                    PointF targetPoint = _currentPath[_currentPathIndex];

                                    if (targetPoint.X < Position.X - 5)
                                    {
                                        VelocityX = -_speed;
                                        _movingRight = false;
                                    }
                                    else if (targetPoint.X > Position.X + 5)
                                    {
                                        VelocityX = _speed;
                                        _movingRight = true;
                                    }
                                    else
                                    {
                                        VelocityX = 0;
                                        _currentPathIndex++;
                                        if (_currentPathIndex >= _currentPath.Count)
                                            _currentPathIndex = -1;
                                    }

                                    if (targetPoint.Y < Position.Y - 10 && _jumpCooldown <= 0 && IsOnGround())
                                    {
                                        VelocityY = -8f; _jumpCooldown = JUMP_COOLDOWN_TIME;
                                        Console.WriteLine("Прыжок вверх для достижения цели!");
                                    }
                                }
                                else
                                {
                                    if (_player.Position.X < Position.X - 5)
                                    {
                                        VelocityX = -_speed;
                                        _movingRight = false;
                                    }
                                    else if (_player.Position.X > Position.X + 5)
                                    {
                                        VelocityX = _speed;
                                        _movingRight = true;
                                    }
                                    else
                                    {
                                        VelocityX = 0;
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (!_isIdle && _preJumpTimer <= 0 && !_isInJumpOverGapMode)
                    {
                        if (IsAtPlatformEdge())
                        {
                            bool isGapJumpable = CheckForGapAhead(_movingRight ? 1 : -1, out float gapWidth);

                            if (isGapJumpable)
                            {
                                _needToJumpOverGap = true;
                                _jumpDistance = gapWidth;
                                _preJumpTimer = PRE_JUMP_TIME;

                                VelocityX = 0;

                                Console.WriteLine($"На краю платформы! Подготовка к прыжку через пропасть шириной {gapWidth}");
                            }
                            else
                            {
                                _movingRight = !_movingRight;
                                _changeDirectionTimer = MIN_DIRECTION_CHANGE_TIME;
                                Console.WriteLine("Пропасть слишком широкая! Разворот");
                            }
                        }
                        else if (_changeDirectionTimer <= 0 && _random.NextDouble() < 0.05)
                        {
                            _isIdle = true;
                            _idleTimer = IDLE_TIME;
                            VelocityX = 0;
                            Console.WriteLine("Переход в режим ожидания");
                        }
                        else if (!_needToJumpOverGap)
                        {
                            VelocityX = _movingRight ? _speed * 0.5f : -_speed * 0.5f;
                        }
                    }
                }
            }

            if (_isInJumpOverGapMode)
            {
                VelocityX = _movingRight ? _speed * _jumpBoostFactor : -_speed * _jumpBoostFactor;

                if (!IsOnGround())
                {
                    VelocityY += 0.35f;
                }
                else
                {
                    _isInJumpOverGapMode = false;
                    Console.WriteLine("Приземление после прыжка через пропасть");
                }
            }
            else if (!IsOnGround())
            {
                float gravityFactor;

                if (VelocityY < -5f)
                {
                    gravityFactor = 0.6f;
                }
                else if (VelocityY < 0)
                {
                    gravityFactor = 0.5f;
                }
                else
                {
                    gravityFactor = 0.5f;
                }

                VelocityY += gravityFactor;
            }
            else
            {
                VelocityY += 0.5f;
            }

            if (VelocityY > 10f)
            {
                VelocityY = 10f;
            }

            Position = new PointF(Position.X + VelocityX, Position.Y + VelocityY);

            if (Position.X < 0)
            {
                Position = new PointF(0, Position.Y);
                _movingRight = true;
                _changeDirectionTimer = MIN_DIRECTION_CHANGE_TIME;
                _needToJumpOverGap = false;
                _preJumpTimer = 0;
            }
            else if (Position.X + Size.Width > 3000)
            {
                Position = new PointF(3000 - Size.Width, Position.Y);
                _movingRight = false;
                _changeDirectionTimer = MIN_DIRECTION_CHANGE_TIME;
                _needToJumpOverGap = false;
                _preJumpTimer = 0;
            }

            if (Position.Y > 1000)
            {
                Reset(camera);
                _needToJumpOverGap = false;
                _preJumpTimer = 0;
                _isInJumpOverGapMode = false;
            }

            Draw(camera);
        }

        protected override bool IsInJumpOverGapMode()
        {
            return _isInJumpOverGapMode;
        }

        public override bool Intersects(GameObject other)
        {
            if (_isInJumpOverGapMode && other is Platform platform)
            {
                RectangleF enemyRect = this.Bounds;
                RectangleF platformRect = platform.Bounds;

                if (VelocityY < 0)
                {
                    if (enemyRect.Top < platformRect.Bottom &&
                        enemyRect.Top > platformRect.Top &&
                        enemyRect.Bottom > platformRect.Bottom &&
                        enemyRect.Right > platformRect.Left + 5 &&
                        enemyRect.Left < platformRect.Right - 5)
                    {
                        return true;
                    }
                }

                if (VelocityY > 0)
                {
                    RectangleF landingRect = new RectangleF(
enemyRect.X - 5, enemyRect.Y + enemyRect.Height - 2,
enemyRect.Width + 10, 6);
                    bool canLand = landingRect.IntersectsWith(platformRect) &&
                                   enemyRect.Bottom < platformRect.Top + 15;
                    return canLand;
                }

                if (VelocityY < 0)
                {
                    if ((_movingRight && platformRect.Left > enemyRect.Left && platformRect.Left < enemyRect.Right) ||
    (!_movingRight && platformRect.Right < enemyRect.Right && platformRect.Right > enemyRect.Left))
                    {
                        return false;
                    }
                }
            }

            return base.Intersects(other);
        }




        private bool CheckForGapAhead(int direction, out float gapWidth)
        {
            gapWidth = 0f;
            if (!IsOnGround()) return false;

            float lookAheadDistance = 5f; float startX = direction > 0 ?
    Position.X + Size.Width + lookAheadDistance :
    Position.X - lookAheadDistance;
            float startY = Position.Y + Size.Height;

            const float CHECK_STEP = 3f; const float MAX_GAP_CHECK_DISTANCE = 400f;
            const float VERTICAL_CHECK_DISTANCE = 150f;
            bool foundGap = false;
            bool foundPlatformAfterGap = false;
            float gapStartX = startX;
            float gapEndX = startX;

            for (float testX = startX;
     Math.Abs(testX - startX) <= MAX_GAP_CHECK_DISTANCE;
     testX += direction * CHECK_STEP)
            {
                bool hasGround = false;

                for (float testY = startY; testY <= startY + VERTICAL_CHECK_DISTANCE; testY += CHECK_STEP)
                {
                    RectangleF testPoint = new RectangleF(testX - 1, testY - 1, 3, 3);

                    foreach (var platform in _gameManager.GetPlatforms())
                    {
                        if (testPoint.IntersectsWith(platform.Bounds))
                        {
                            hasGround = true;
                            break;
                        }
                    }

                    if (hasGround) break;
                }

                if (!hasGround && !foundGap)
                {
                    foundGap = true;
                    gapStartX = testX;
                }
                else if (hasGround && foundGap && !foundPlatformAfterGap)
                {
                    foundPlatformAfterGap = true;
                    gapEndX = testX;
                    break;
                }
            }

            if (foundGap && foundPlatformAfterGap)
            {
                gapWidth = Math.Abs(gapEndX - gapStartX);

                float jumpBoostFactor = 2.0f;
                if (gapWidth < 50f) jumpBoostFactor = 1.8f; else if (gapWidth > 100f) jumpBoostFactor = 2.3f;
                float maxJumpableGap = _speed * jumpBoostFactor * 25f;

                if (gapWidth < 20f)
                {
                    return DetectPlayer(200f);
                }

                return gapWidth <= maxJumpableGap;
            }

            return false;
        }

        private bool IsOnGround()
        {
            RectangleF testRect = new RectangleF(
                Position.X, Position.Y + Size.Height + 1,
                Size.Width, 2);

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

                    if (_breathingOffset != 0)
                    {
                        int originalHeight = 32;
                        int newHeight = originalHeight + _breathingOffset;
                        Sprite.Height = newHeight;

                        Sprite.Location = new Point(
                            (int)screenPos.X,
                            (int)screenPos.Y + (originalHeight - newHeight) / 2);
                    }
                    else if (_preJumpTimer > 0)
                    {
                        Sprite.Height = 28;
                        Sprite.Location = new Point((int)screenPos.X, (int)screenPos.Y + 4);
                    }
                    else if (_needToJumpOverGap && IsOnGround())
                    {
                        Sprite.Height = 30;
                        Sprite.Location = new Point((int)screenPos.X, (int)screenPos.Y + 2);
                    }
                    else if (!IsOnGround())
                    {
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
                    }
                    else
                    {
                        Sprite.Location = new Point((int)screenPos.X, (int)screenPos.Y);
                        Sprite.Height = 32;
                    }
                    Sprite.Visible = true;
                }
                else
                {
                    Sprite.Visible = false;
                }
            }
        }

        private bool IsAtPlatformEdge()
        {
            float[] probeDistances = { 10f, 20f, 40f };
            foreach (float probeDistance in probeDistances)
            {
                float probeX = _movingRight ?
                    Position.X + Size.Width + probeDistance :
                    Position.X - probeDistance;

                float probeY = Position.Y + Size.Height + 5;

                RectangleF probe1 = new RectangleF(probeX, probeY, 5, 5); RectangleF probe2 = new RectangleF(probeX, probeY - 10, 5, 5);
                bool foundGround = false;

                foreach (var obj in _gameManager.GetPlatforms())
                {
                    if (probe1.IntersectsWith(obj.Bounds) || probe2.IntersectsWith(obj.Bounds))
                    {
                        foundGround = true;
                        break;
                    }
                }

                if (probeX < 0 || probeX > 3000)
                {
                    return true;
                }

                if (!foundGround)
                {
                    if (probeDistance <= 20f)
                    {
                        return true;
                    }

                    bool playerIsClose = DetectPlayer(250f);
                    if (playerIsClose)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool DetectPlayerWithHysteresis(float baseRange, float hysteresis)
        {
            if (_player == null) return false;

            float distanceX = Math.Abs(_player.Position.X - Position.X);
            float distanceY = Math.Abs(_player.Position.Y - Position.Y);

            float effectiveRange = _isChasing ? baseRange + hysteresis : baseRange;

            bool inRange = distanceX < effectiveRange && distanceY < effectiveRange * 0.75f;

            if (!inRange && distanceX < effectiveRange * 1.5f && distanceY < effectiveRange * 1.0f)
            {
                bool hasLineOfSight = CheckLineOfSight(_player.Position);
                if (hasLineOfSight)
                {
                    return true;
                }
            }

            return inRange;
        }

        private bool CheckLineOfSight(PointF targetPosition)
        {
            PointF start = new PointF(Position.X + Size.Width / 2, Position.Y + Size.Height / 2);
            PointF end = new PointF(targetPosition.X + _player.Size.Width / 2, targetPosition.Y + _player.Size.Height / 2);

            float dx = end.X - start.X;
            float dy = end.Y - start.Y;
            float distance = (float)Math.Sqrt(dx * dx + dy * dy);

            float nx = dx / distance;
            float ny = dy / distance;

            if (Math.Abs(ny) < 0.3f && Math.Abs(dx) > 30f)
            {
                int direction = nx > 0 ? 1 : -1; bool isGapAhead = IsGapBetweenPoints(start, end);

                if (isGapAhead)
                {
                    Console.WriteLine("КРИТИЧЕСКАЯ ПРОВЕРКА: Обнаружена яма между врагом и игроком!");
                    return false;
                }
            }

            int steps = 30; for (int i = 1; i < steps; i++)
            {
                float t = i / (float)steps;
                PointF checkPoint = new PointF(
                    start.X + nx * distance * t,
                    start.Y + ny * distance * t
                );

                RectangleF checkRect = new RectangleF(checkPoint.X - 1, checkPoint.Y - 1, 2, 2);

                foreach (var platform in _gameManager.GetPlatforms())
                {
                    if (checkRect.IntersectsWith(platform.Bounds))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private bool IsGapBetweenPoints(PointF start, PointF end)
        {
            int direction = start.X < end.X ? 1 : -1;
            float distance = Math.Abs(end.X - start.X);
            int steps = Math.Max(5, (int)(distance / 10));
            float stepSize = distance / steps;
            float currentX = start.X;

            for (int i = 0; i <= steps; i++)
            {
                currentX += direction * stepSize;

                if ((direction > 0 && currentX >= end.X) ||
                    (direction < 0 && currentX <= end.X))
                    break;

                bool foundGround = false;

                RectangleF groundCheck = new RectangleF(
    currentX - 2,
    Math.Min(start.Y, end.Y),
    4,
    Math.Abs(start.Y - end.Y) + 200
);

                foreach (var platform in _gameManager.GetPlatforms())
                {
                    if (groundCheck.IntersectsWith(platform.Bounds))
                    {
                        foundGround = true;
                        break;
                    }
                }

                if (!foundGround)
                {
                    return true;
                }
            }

            return false;
        }

        private bool MoveDirectlyTowardsPlayer()
        {
            if (_player == null) return false;

            float horizontalDistance = Math.Abs(_player.Position.X - Position.X);
            float verticalDistance = _player.Position.Y - Position.Y;

            PointF startPoint = new PointF(Position.X + Size.Width / 2, Position.Y + Size.Height / 2);
            PointF endPoint = new PointF(_player.Position.X + _player.Size.Width / 2,
                                        _player.Position.Y + _player.Size.Height / 2);
            bool hasGapBetweenUs = IsGapBetweenPoints(startPoint, endPoint);

            if (hasGapBetweenUs)
            {
                Console.WriteLine("БЛОКИРОВКА прямого движения - обнаружена яма между врагом и игроком");
                return false;
            }

            if (verticalDistance < -30 && horizontalDistance < 150 && IsOnGround() && _jumpCooldown <= 0)
            {
                VelocityY = -10f; _jumpCooldown = JUMP_COOLDOWN_TIME;

                if (_player.Position.X < Position.X)
                {
                    VelocityX = -_speed;
                    _movingRight = false;
                }
                else
                {
                    VelocityX = _speed;
                    _movingRight = true;
                }

                Console.WriteLine("Прыжок вверх к игроку!");
                return true;
            }

            if (Math.Abs(verticalDistance) < 50 && CheckLineOfSight(_player.Position))
            {
                bool moveRight = _player.Position.X > Position.X + 5;
                bool moveLeft = _player.Position.X < Position.X - 5;

                if (moveLeft)
                {
                    VelocityX = -_speed;
                    _movingRight = false;
                    return true;
                }
                else if (moveRight)
                {
                    VelocityX = _speed;
                    _movingRight = true;
                    return true;
                }

                VelocityX = 0;
                return true;
            }

            return false;
        }

        private bool ShouldJumpToReachPlayer()
        {
            if (_player == null || !IsOnGround() || _jumpCooldown > 0)
                return false;

            float yDifference = Position.Y - _player.Position.Y;
            float xDistance = Math.Abs(Position.X - _player.Position.X);

            float maxJumpableHeight = 150f;
            if (yDifference > maxJumpableHeight)
            {
                return false;
            }

            return yDifference > 30 && xDistance < 150;
        }

        private static readonly Random _random = new Random();
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