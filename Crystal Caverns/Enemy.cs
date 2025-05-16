using System;
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

        protected bool DetectPlayer(float detectionRange)
        {
            if (_player == null) return false;

            float distanceX = Math.Abs(_player.Position.X - Position.X);
            float distanceY = Math.Abs(_player.Position.Y - Position.Y);

            return distanceX < detectionRange && distanceY < detectionRange / 2;
        }

        public override void OnCollision(GameObject other)
        {
            if (other is Platform platform)
            {
                HandlePlatformCollision(platform);
            }
        }

        protected virtual void HandlePlatformCollision(Platform platform)
        {
            RectangleF enemyRect = Bounds;
            RectangleF platformRect = platform.Bounds;

            bool isAbovePlatform = enemyRect.Bottom >= platformRect.Top &&
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

        private float _changeDirectionTimer = 0;
        private float _idleTimer = 0;
        private bool _isIdle = false;
        private const float MIN_DIRECTION_CHANGE_TIME = 0.5f; 
        private const float IDLE_TIME = 1.0f; 
        private const float CHASE_HYSTERESIS = 20.0f; 

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

        public override void Update(GameTime gameTime, Camera camera)
        {
            
            _changeDirectionTimer -= gameTime.DeltaTime;

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
                }

                if (_isChasing && _player != null)
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
                else
                {
                    if (!_isIdle)
                    {
                        if (IsAtPlatformEdge())
                        {
                            
                            _movingRight = !_movingRight;
                            _changeDirectionTimer = MIN_DIRECTION_CHANGE_TIME;
                            Console.WriteLine("Враг достиг края платформы и повернул");
                        }
                    }

                    if (_changeDirectionTimer <= 0 && _random.NextDouble() < 0.05) 
                    {
                        _isIdle = true;
                        _idleTimer = IDLE_TIME;
                        VelocityX = 0;
                    }
                    else
                    {
                        VelocityX = _movingRight ? _speed * 0.5f : -_speed * 0.5f;
                    }
                }
            }

            VelocityY += 0.5f;
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
            }
            else if (Position.X + Size.Width > 800) 
            {
                Position = new PointF(800 - Size.Width, Position.Y);
                _movingRight = false;
                _changeDirectionTimer = MIN_DIRECTION_CHANGE_TIME;
            }

            if (Position.Y > 800) 
            {
                Reset(camera);
            }

            Draw(camera);
        }
        private bool IsAtPlatformEdge()
        {
            float probeX = _movingRight ?
                Position.X + Size.Width + 5 : 
                Position.X - 5;               

            float probeY = Position.Y + Size.Height + 5; 

            RectangleF probe = new RectangleF(probeX, probeY, 5, 5);

            bool foundGround = false;

            foreach (var obj in _gameManager.GetPlatforms())
            {
                if (probe.IntersectsWith(obj.Bounds))
                {
                    foundGround = true;
                    break;
                }
            }

            if (probeX < 0 || probeX > 800) 
            {
                return true; 
            }

            return !foundGround;
        }

        private bool DetectPlayerWithHysteresis(float baseRange, float hysteresis)
        {
            if (_player == null) return false;

            float distanceX = Math.Abs(_player.Position.X - Position.X);
            float distanceY = Math.Abs(_player.Position.Y - Position.Y);

            float effectiveRange = _isChasing ? baseRange + hysteresis : baseRange;

            return distanceX < effectiveRange && distanceY < effectiveRange / 2;
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