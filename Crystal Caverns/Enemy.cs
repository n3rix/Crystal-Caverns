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
            // Если это ChasingEnemy и он в режиме прыжка через пропасть, особая обработка

            if (VelocityY < 0) // Движемся вверх
            {
                if (enemyRect.Top < platformRect.Bottom &&
                    enemyRect.Top > platformRect.Top &&
                    enemyRect.Bottom > platformRect.Bottom &&
                    enemyRect.Right > platformRect.Left + 5 &&
                    enemyRect.Left < platformRect.Right - 5)
                {
                    // Столкновение с потолком - обрабатываем его в первую очередь
                    Position = new PointF(Position.X, platformRect.Bottom);
                    VelocityY = 0.5f; // Небольшая положительная скорость вниз после удара
                    Console.WriteLine("Удар головой о платформу!");
                    return; // Важно! Возвращаемся сразу после обработки удара головой
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

            // Если враг прыгает (но не в режиме прыжка через пропасть), также особая обработка
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

            // Обычная обработка коллизий для всех других случаев
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

        private const float JUMP_FORCE_BASE = 8.0f; // Увеличиваем базовую силу прыжка (было 10.0f)
        private const float JUMP_FORCE_MAX = 10.0f;  // Увеличиваем максимальную силу прыжка (было 15.0f)
        private const float PRE_JUMP_ACCELERATION = 1.0f; // Добавляем ускорение перед прыжком
        private float _preJumpTimer = 0f; // Таймер для разгона перед прыжком
        private const float PRE_JUMP_TIME = 0.2f; // Время для разгона перед прыжком

        private bool _isInJumpOverGapMode = false; // Режим прыжка через пропасть
        private float _jumpBoostFactor = 1f;
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

            // Инициализируем граф навигации при создании врага
            _navigationGraph = new NavigationGraph(_gameManager.GetPlatforms());
        }

        public override void Update(GameTime gameTime, Camera camera)
        {
            // Обновляем таймеры
            _changeDirectionTimer -= gameTime.DeltaTime;
            _pathUpdateTimer -= gameTime.DeltaTime;
            if (_jumpCooldown > 0)
                _jumpCooldown -= gameTime.DeltaTime;
            // Обновляем таймер разгона перед прыжком
            if (_gapJumpTimer > 0)
            {
                _gapJumpTimer -= gameTime.DeltaTime;
            }
            if (_preJumpTimer > 0)
            {
                _preJumpTimer -= gameTime.DeltaTime;
                // Разгоняемся перед прыжком
                VelocityX += _movingRight ? PRE_JUMP_ACCELERATION : -PRE_JUMP_ACCELERATION;

                // Ограничиваем максимальную скорость разгона
                if (_movingRight && VelocityX > _speed * 1.5f)
                    VelocityX = _speed * 1.5f;
                else if (!_movingRight && VelocityX < -_speed * 1.5f)
                    VelocityX = -_speed * 1.5f;

                // Когда таймер истек, выполняем прыжок через пропасть
                // В методе Update, где выполняется прыжок через пропасть:
                if (_preJumpTimer <= 0 && IsOnGround() && _jumpCooldown <= 0 && _needToJumpOverGap)
                {
                    // ИЗМЕНЕНИЕ: Улучшенная формула расчета силы прыжка
                    // Базовая сила прыжка с плавной зависимостью от ширины ямы
                    float jumpForceBase = JUMP_FORCE_BASE;
                    float jumpForceExtraForDistance = 0;

                    // Для маленьких ям (меньше 50px) используем сниженную силу прыжка
                    if (_jumpDistance < 50f)
                    {
                        jumpForceBase = JUMP_FORCE_BASE * 0.75f;
                        _jumpBoostFactor = 1.8f; // Меньший горизонтальный импульс
                    }
                    // Для средних ям (50-100px) используем стандартную силу прыжка
                    else if (_jumpDistance < 100f)
                    {
                        jumpForceBase = JUMP_FORCE_BASE;
                        jumpForceExtraForDistance = (_jumpDistance - 50f) / 50f; // 0-1 дополнительная сила
                        _jumpBoostFactor = 2.0f;
                    }
                    // Для широких ям (более 100px) используем увеличенную силу прыжка
                    else
                    {
                        jumpForceBase = JUMP_FORCE_BASE * 1.1f;
                        jumpForceExtraForDistance = (_jumpDistance - 100f) / 100f; // Дополнительная сила
                        jumpForceExtraForDistance = Math.Min(jumpForceExtraForDistance, 1.0f); // Максимум +1
                        _jumpBoostFactor = 2.2f + (jumpForceExtraForDistance * 0.5f); // Увеличенный горизонтальный импульс
                    }

                    // Финальный расчет силы прыжка
                    float jumpForce = jumpForceBase + jumpForceExtraForDistance;

                    // Ограничиваем максимальную силу прыжка
                    jumpForce = Math.Min(jumpForce, JUMP_FORCE_MAX);

                    // Чтобы избежать проваливания, отступаем немного от края
                    Position = new PointF(Position.X - (_movingRight ? -3f : 3f), Position.Y);

                    VelocityY = -jumpForce;

                    // Горизонтальный импульс, масштабированный для ширины ямы
                    VelocityX = _movingRight ? _speed * _jumpBoostFactor : -_speed * _jumpBoostFactor;

                    // Активируем режим прыжка
                    _isInJumpOverGapMode = true;
                    _jumpCooldown = JUMP_COOLDOWN_TIME;
                    _needToJumpOverGap = false;

                    // Длительный таймер режима прыжка, масштабированный для ширины ямы
                    _gapJumpTimer = 0.5f + (_jumpDistance / 100f); // 0.5с для короткой ямы, больше для длинной

                    Console.WriteLine($"ПРЫЖОК! Сила={jumpForce}, Скорость X={VelocityX}, JumpBoostFactor={_jumpBoostFactor}");
                }
            }

            if (_isIdle)
            {
                // Код для режима ожидания без изменений...
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

                    // Если начали погоню, обновляем путь
                    if (_isChasing)
                    {
                        _pathUpdateTimer = 0; // Заставляем обновить путь немедленно
                    }
                }

                if (_isChasing && _player != null)
                {
                    if (IsOnGround() && !_needToJumpOverGap && !_isInJumpOverGapMode)
                    {
                        // НОВОЕ: Всегда сначала проверяем наличие ямы, даже до выбора стратегии
                        int direction = _player.Position.X > Position.X ? 1 : -1;
                        bool isGapAhead = CheckForGapAhead(direction, out float gapWidth);

                        if (isGapAhead)
                        {
                            _needToJumpOverGap = true;
                            _jumpDistance = gapWidth;
                            // Начинаем подготовку к прыжку
                            _preJumpTimer = PRE_JUMP_TIME;
                            _movingRight = direction > 0;
                            VelocityX = _movingRight ? Math.Max(_speed, _minJumpSpeed) : Math.Min(-_speed, -_minJumpSpeed);
                            Console.WriteLine($"ПРИОРИТЕТНАЯ ПРОВЕРКА: Обнаружена пропасть шириной {gapWidth}! Готовимся к прыжку.");
                        }
                    }
                    // Обновляем путь по таймеру
                    bool directMovePossible = MoveDirectlyTowardsPlayer();
                    if (ShouldJumpToReachPlayer() && _jumpCooldown <= 0 && IsOnGround())
                    {
                        // Прыгаем вверх с большой силой
                        VelocityY = -10f;
                        _jumpCooldown = JUMP_COOLDOWN_TIME;

                        // Двигаемся в сторону игрока по горизонтали
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
                    // Если прямое движение невозможно, используем навигацию
                    if (!directMovePossible)
                    {
                        // Обновляем путь по таймеру
                        if (_pathUpdateTimer <= 0)
                        {
                            _currentPath = _navigationGraph.FindPath(Position, _player.Position);
                            _currentPathIndex = _currentPath.Count > 1 ? 1 : -1;
                            _pathUpdateTimer = PATH_UPDATE_INTERVAL;
                        }

                        // Следуем по пути только если не готовимся к прыжку и не находимся в режиме прыжка
                        if (_preJumpTimer <= 0 && !_isInJumpOverGapMode)
                        {
                            // Проверяем наличие пропасти перед движением
                            if (IsOnGround() && !_needToJumpOverGap)
                            {
                                // Используем направление движения для определения, где проверять пропасть
                                bool isGapAhead = CheckForGapAhead(_movingRight ? 1 : -1, out float gapWidth);
                                if (isGapAhead)
                                {
                                    _needToJumpOverGap = true;
                                    _jumpDistance = gapWidth;
                                    // Начинаем подготовку к прыжку
                                    _preJumpTimer = PRE_JUMP_TIME;
                                    VelocityX = _movingRight ? Math.Max(_speed, _minJumpSpeed) : Math.Min(-_speed, -_minJumpSpeed);
                                    Console.WriteLine($"Обнаружена пропасть шириной {gapWidth}! Готовимся к прыжку.");
                                }
                            }
                            else // Только если впереди нет пропасти, следуем по пути
                            {
                                // Следуем по пути
                                if (_currentPathIndex >= 0 && _currentPathIndex < _currentPath.Count)
                                {
                                    PointF targetPoint = _currentPath[_currentPathIndex];

                                    // Решаем, двигаться влево или вправо
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
                                        // Если достигли текущей точки пути, переходим к следующей
                                        _currentPathIndex++;
                                        if (_currentPathIndex >= _currentPath.Count)
                                            _currentPathIndex = -1;
                                    }

                                    // Решаем, нужно ли прыгать для достижения цели по вертикали
                                    if (targetPoint.Y < Position.Y - 10 && _jumpCooldown <= 0 && IsOnGround())
                                    {
                                        VelocityY = -8f; // Сила прыжка вверх
                                        _jumpCooldown = JUMP_COOLDOWN_TIME;
                                        Console.WriteLine("Прыжок вверх для достижения цели!");
                                    }
                                }
                                else // Если нет пути, просто следуем за игроком
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
                else // Режим патрулирования
                {
                    // Код для режима патрулирования...
                    if (!_isIdle && _preJumpTimer <= 0 && !_isInJumpOverGapMode)
                    {
                        // Проверяем края платформы и пропасти
                        if (IsAtPlatformEdge())
                        {
                            // При обнаружении края СРАЗУ проверяем, можно ли перепрыгнуть пропасть
                            bool isGapJumpable = CheckForGapAhead(_movingRight ? 1 : -1, out float gapWidth);

                            // Если пропасть можно перепрыгнуть, готовимся к прыжку
                            if (isGapJumpable)
                            {
                                _needToJumpOverGap = true;
                                _jumpDistance = gapWidth;
                                _preJumpTimer = PRE_JUMP_TIME;

                                // Остановиться на краю для подготовки к прыжку
                                VelocityX = 0;

                                Console.WriteLine($"На краю платформы! Подготовка к прыжку через пропасть шириной {gapWidth}");
                            }
                            else
                            {
                                // Если пропасть слишком широкая, разворачиваемся
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
                        else if (!_needToJumpOverGap) // Только если не готовимся к прыжку
                        {
                            VelocityX = _movingRight ? _speed * 0.5f : -_speed * 0.5f;
                        }
                    }
                }
            }

            // Обработка гравитации и горизонтальной скорости для режима прыжка через пропасть
            if (_isInJumpOverGapMode)
            {
                // Поддерживаем высокую горизонтальную скорость во время прыжка
                VelocityX = _movingRight ? _speed * _jumpBoostFactor : -_speed * _jumpBoostFactor;

                // Уменьшаем гравитацию во время прыжка для увеличения дальности
                if (!IsOnGround())
                {
                    VelocityY += 0.35f; // Очень низкая гравитация для дальнего прыжка
                }
                else
                {
                    // При приземлении отключаем режим прыжка
                    _isInJumpOverGapMode = false;
                    Console.WriteLine("Приземление после прыжка через пропасть");
                }
            }
            else if (!IsOnGround()) // Обычная гравитация для прыжков
            {
                float gravityFactor;

                // Если враг прыгнул слишком высоко, увеличиваем гравитацию
                if (VelocityY < -5f)
                {
                    gravityFactor = 0.6f; // Увеличиваем с 0.4f до 0.6f для быстрого падения
                }
                else if (VelocityY < 0)
                {
                    gravityFactor = 0.5f; // Стандартная гравитация при подъеме
                }
                else
                {
                    gravityFactor = 0.5f; // Стандартная гравитация при падении
                }

                VelocityY += gravityFactor;
            }
            else // Нормальная гравитация на земле
            {
                VelocityY += 0.5f;
            }

            // Ограничение скорости падения
            if (VelocityY > 10f)
            {
                VelocityY = 10f;
            }

            // Обновляем позицию
            Position = new PointF(Position.X + VelocityX, Position.Y + VelocityY);

            // Проверка границ уровня и остальной код без изменений...
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

            // Проверка падения
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
            // Если мы в процессе прыжка через пропасть и объект - платформа
            if (_isInJumpOverGapMode && other is Platform platform)
            {
                RectangleF enemyRect = this.Bounds;
                RectangleF platformRect = platform.Bounds;

                // КРИТИЧЕСКАЯ ПРОВЕРКА ПОТОЛКА: всегда проверяем столкновение с потолком
                if (VelocityY < 0) // Движемся вверх
                {
                    if (enemyRect.Top < platformRect.Bottom &&
                        enemyRect.Top > platformRect.Top &&
                        enemyRect.Bottom > platformRect.Bottom &&
                        enemyRect.Right > platformRect.Left + 5 &&
                        enemyRect.Left < platformRect.Right - 5)
                    {
                        return true; // Столкновение с потолком всегда обрабатываем
                    }
                }

                // Во время падения проверяем все платформы под нами
                if (VelocityY > 0)
                {
                    // Ищем платформы для приземления во время падения 
                    // Используем увеличенную область проверки для не пропуска платформ
                    RectangleF landingRect = new RectangleF(
                        enemyRect.X - 5, enemyRect.Y + enemyRect.Height - 2,
                        enemyRect.Width + 10, 6); // Увеличенная ширина и высота для лучшего обнаружения

                    bool canLand = landingRect.IntersectsWith(platformRect) &&
                                   enemyRect.Bottom < platformRect.Top + 15; // Проверка что мы над платформой

                    return canLand;
                }

                // Игнорируем только боковые столкновения при движении вверх
                if (VelocityY < 0)
                {
                    // Игнорируем только боковые столкновения
                    if ((_movingRight && platformRect.Left > enemyRect.Left && platformRect.Left < enemyRect.Right) ||
                        (!_movingRight && platformRect.Right < enemyRect.Right && platformRect.Right > enemyRect.Left))
                    {
                        return false;
                    }
                }
            }

            // Во всех остальных случаях стандартное поведение
            return base.Intersects(other);
        }




        private bool CheckForGapAhead(int direction, out float gapWidth)
        {
            gapWidth = 0f;
            if (!IsOnGround()) return false;

            // Определяем начальную позицию для проверки
            float lookAheadDistance = 10f;
            float startX = direction > 0 ?
                Position.X + Size.Width + lookAheadDistance :
                Position.X - lookAheadDistance;
            float startY = Position.Y + Size.Height;

            // Константы для проверки
            const float MAX_GAP_CHECK_DISTANCE = 400f;
            const float CHECK_STEP = 5f;
            const float VERTICAL_CHECK_DISTANCE = 120f;

            bool foundGap = false;
            bool foundPlatformAfterGap = false;
            float gapStartX = startX;
            float gapEndX = startX;

            // Проверяем наличие земли впереди
            for (float testX = startX;
                 Math.Abs(testX - startX) <= MAX_GAP_CHECK_DISTANCE;
                 testX += direction * CHECK_STEP)
            {
                bool hasGround = false;

                // Проверяем наличие земли
                for (float testY = startY; testY <= startY + VERTICAL_CHECK_DISTANCE; testY += CHECK_STEP)
                {
                    RectangleF testPoint = new RectangleF(testX, testY, 2, 2);

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

                // Если нашли начало пропасти
                if (!hasGround && !foundGap)
                {
                    foundGap = true;
                    gapStartX = testX;
                }
                // Если нашли конец пропасти
                else if (hasGround && foundGap && !foundPlatformAfterGap)
                {
                    foundPlatformAfterGap = true;
                    gapEndX = testX;
                    break;
                }
            }

            // Если нашли пропасть и платформу за ней
            if (foundGap && foundPlatformAfterGap)
            {
                gapWidth = Math.Abs(gapEndX - gapStartX);
                Console.WriteLine($"Обнаружена пропасть шириной {gapWidth}");

                // ИЗМЕНЕНИЕ: Динамический расчет максимально преодолимой ширины ямы
                // Зависит от скорости врага, множителя прыжка и фактора силы
                float maxJumpableGap = _speed * _jumpBoostFactor * 25f;

                // Дополнительное ограничение для очень маленьких ям
                if (gapWidth < 20f)
                {
                    // Очень маленькие ямы тоже опасны - преодолевать только близко к игроку
                    bool playerIsClose = DetectPlayer(200f);
                    return playerIsClose; // Прыгаем через мелкие ямы только при преследовании
                }

                // Для нормальных ям используем новую формулу
                return gapWidth <= maxJumpableGap;
            }

            return false;
        }

        // Проверяет, стоит ли враг на земле
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
                        // Визуализируем подготовку к прыжку - приседание сильнее
                        Sprite.Height = 28;
                        Sprite.Location = new Point((int)screenPos.X, (int)screenPos.Y + 4);
                    }
                    else if (_needToJumpOverGap && IsOnGround())
                    {
                        // Визуализируем подготовку к прыжку - приседание
                        Sprite.Height = 30;
                        Sprite.Location = new Point((int)screenPos.X, (int)screenPos.Y + 2);
                    }
                    else if (!IsOnGround())
                    {
                        if (VelocityY < 0)
                        {
                            // Прыжок - растягиваем спрайт вверх
                            Sprite.Height = 36;
                            Sprite.Location = new Point((int)screenPos.X, (int)screenPos.Y - 4);
                        }
                        else
                        {
                            // Падение - растягиваем спрайт вниз
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
            // ИЗМЕНЕНИЕ: Улучшенная проверка края платформы
            // Проверяем несколько точек на разных расстояниях
            float[] probeDistances = { 10f, 20f, 40f }; // Проверяем на трех расстояниях

            foreach (float probeDistance in probeDistances)
            {
                float probeX = _movingRight ?
                    Position.X + Size.Width + probeDistance :
                    Position.X - probeDistance;

                float probeY = Position.Y + Size.Height + 5;

                // Используем две точки проверки по вертикали для большей надежности
                RectangleF probe1 = new RectangleF(probeX, probeY, 5, 5); // Нижняя проверка
                RectangleF probe2 = new RectangleF(probeX, probeY - 10, 5, 5); // Верхняя проверка

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

                // Если на этом расстоянии нет земли, это край или пропасть
                if (!foundGround)
                {
                    // Если нашли край на малом расстоянии - это явный признак края
                    if (probeDistance <= 20f)
                    {
                        return true;
                    }

                    // На дальних дистанциях проверяем есть ли игрок поблизости
                    // Если игрок близко, реагируем быстрее на дальние ямы
                    bool playerIsClose = DetectPlayer(250f);
                    if (playerIsClose)
                    {
                        return true;
                    }
                }
            }

            // Если ни одна проверка не нашла край, значит край не обнаружен
            return false;
        }

        // В методе DetectPlayerWithHysteresis класса ChasingEnemy добавь новую логику для обнаружения через объекты:
        private bool DetectPlayerWithHysteresis(float baseRange, float hysteresis)
        {
            if (_player == null) return false;

            float distanceX = Math.Abs(_player.Position.X - Position.X);
            float distanceY = Math.Abs(_player.Position.Y - Position.Y);

            float effectiveRange = _isChasing ? baseRange + hysteresis : baseRange;

            // Базовая проверка расстояния
            bool inRange = distanceX < effectiveRange && distanceY < effectiveRange * 0.75f;

            // Если игрок в пределах увеличенного диапазона, проверяем прямую видимость
            if (!inRange && distanceX < effectiveRange * 1.5f && distanceY < effectiveRange * 1.0f)
            {
                // Дополнительная проверка на прямую видимость для увеличенной дальности
                // Это позволит врагу видеть игрока на большем расстоянии, если между ними нет препятствий
                bool hasLineOfSight = CheckLineOfSight(_player.Position);
                if (hasLineOfSight)
                {
                    return true;
                }
            }

            return inRange;
        }

        // Добавь новый метод для проверки прямой видимости:
        private bool CheckLineOfSight(PointF targetPosition)
        {
            // Начальная и конечная точки луча
            PointF start = new PointF(Position.X + Size.Width / 2, Position.Y + Size.Height / 2);
            PointF end = new PointF(targetPosition.X + _player.Size.Width / 2, targetPosition.Y + _player.Size.Height / 2);

            // Направление и расстояние
            float dx = end.X - start.X;
            float dy = end.Y - start.Y;
            float distance = (float)Math.Sqrt(dx * dx + dy * dy);

            // Нормализованное направление
            float nx = dx / distance;
            float ny = dy / distance;

            // КРИТИЧЕСКОЕ ИЗМЕНЕНИЕ: Всегда проверяем ямы при горизонтальном движении
            if (Math.Abs(ny) < 0.3f && Math.Abs(dx) > 30f) // Если почти горизонтальное движение
            {
                // Проверка наличия ямы между нами и игроком
                int direction = nx > 0 ? 1 : -1; // Направление к игроку
                bool isGapAhead = IsGapBetweenPoints(start, end);

                if (isGapAhead)
                {
                    Console.WriteLine("КРИТИЧЕСКАЯ ПРОВЕРКА: Обнаружена яма между врагом и игроком!");
                    return false; // Определенно есть яма между нами
                }
            }

            // Стандартная проверка препятствий
            int steps = 30; // Увеличиваем количество проверяемых точек
            for (int i = 1; i < steps; i++)
            {
                float t = i / (float)steps;
                PointF checkPoint = new PointF(
                    start.X + nx * distance * t,
                    start.Y + ny * distance * t
                );

                // Проверяем, не пересекает ли точка какую-либо платформу
                RectangleF checkRect = new RectangleF(checkPoint.X - 1, checkPoint.Y - 1, 2, 2);

                foreach (var platform in _gameManager.GetPlatforms())
                {
                    if (checkRect.IntersectsWith(platform.Bounds))
                    {
                        return false; // Есть препятствие на пути
                    }
                }
            }

            return true; // Нет препятствий на пути
        }

        // 3. НОВЫЙ МЕТОД: Более надежная проверка ям между двумя точками
        private bool IsGapBetweenPoints(PointF start, PointF end)
        {
            // Направление по горизонтали
            int direction = start.X < end.X ? 1 : -1;

            // Определяем количество шагов проверки (более частые шаги)
            float distance = Math.Abs(end.X - start.X);
            int steps = Math.Max(5, (int)(distance / 10)); // Проверка через каждые 15 пикселей

            float stepSize = distance / steps;

            // Отступаем немного от начальной позиции, чтобы не проверять платформу под врагом
            float currentX = start.X + (direction * Size.Width / 2);

            for (int i = 0; i <= steps; i++)
            {
                currentX += direction * stepSize;

                // Если дошли до конечной точки, заканчиваем
                if ((direction > 0 && currentX >= end.X) ||
                    (direction < 0 && currentX <= end.X))
                    break;

                // Проверяем наличие земли под этой точкой
                bool foundGround = false;

                // Создаем вертикальный луч вниз от этой точки для проверки наличия платформы
                RectangleF groundCheck = new RectangleF(
                    currentX - 2, // Ширина проверки 4 пикселя
                    Math.Min(start.Y, end.Y), // Начинаем с минимальной высоты
                    4,
                    Math.Abs(start.Y - end.Y) + 200 // Проверяем до нужной глубины
                );

                foreach (var platform in _gameManager.GetPlatforms())
                {
                    if (groundCheck.IntersectsWith(platform.Bounds))
                    {
                        foundGround = true;
                        break;
                    }
                }

                // Если в этой точке нет земли - это яма
                if (!foundGround)
                {
                    Console.WriteLine($"Нашли яму между точками на X = {currentX}");
                    return true;
                }
            }

            // Не нашли ям между точками
            return false;
        }

        private bool MoveDirectlyTowardsPlayer()
        {
            if (_player == null) return false;

            // Проверяем расстояние по горизонтали и вертикали
            float horizontalDistance = Math.Abs(_player.Position.X - Position.X);
            float verticalDistance = _player.Position.Y - Position.Y;

            // Всегда проверяем наличие ям между нами и игроком!
            PointF startPoint = new PointF(Position.X + Size.Width / 2, Position.Y + Size.Height / 2);
            PointF endPoint = new PointF(_player.Position.X + _player.Size.Width / 2,
                                        _player.Position.Y + _player.Size.Height / 2);
            bool hasGapBetweenUs = IsGapBetweenPoints(startPoint, endPoint);

            // Если есть яма - не идем напрямую!
            if (hasGapBetweenUs)
            {
                Console.WriteLine("БЛОКИРОВКА прямого движения - обнаружена яма между врагом и игроком");
                return false;
            }

            // Если игрок выше и близко по горизонтали - приоритет прыжка вверх
            if (verticalDistance < -30 && horizontalDistance < 150 && IsOnGround() && _jumpCooldown <= 0)
            {
                // Прыгаем вверх к игроку
                VelocityY = -10f; // Сильный прыжок
                _jumpCooldown = JUMP_COOLDOWN_TIME;

                // Двигаемся к точке под игроком
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

            // Если на одном уровне - проверяем возможность прямого движения
            if (Math.Abs(verticalDistance) < 50 && CheckLineOfSight(_player.Position))
            {
                // Определяем направление к игроку
                bool moveRight = _player.Position.X > Position.X + 5;
                bool moveLeft = _player.Position.X < Position.X - 5;

                // Если нет ямы на пути, можем двигаться напрямую
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

            // Проверяем, находится ли игрок выше нас и не слишком далеко по горизонтали
            float yDifference = Position.Y - _player.Position.Y;
            float xDistance = Math.Abs(Position.X - _player.Position.X);

            // Проверка, что игрок не слишком высоко 
            float maxJumpableHeight = 150f; // Максимальная высота, на которую враг может запрыгнуть

            // Если игрок слишком высоко, не пытаемся прыгать
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