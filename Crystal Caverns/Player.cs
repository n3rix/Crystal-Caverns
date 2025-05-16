using System;
using System.Drawing;
using System.Windows.Forms;
using Crystal_Caverns.Utils;

namespace Crystal_Caverns.Model
{
    public class Player : GameObject
    {
        private const float MOVE_SPEED = 5.0f;
        private const float JUMP_FORCE = -12.0f;
        private const float GRAVITY = 0.5f;
        private const float MAX_FALL_SPEED = 12.0f;

        public int Lives { get; private set; }
        public int Score { get; private set; }
        public bool IsOnGround { get; private set; }
        public bool IsJumping { get; private set; }
        public bool CanDoubleJump { get; private set; }

        private bool _movingLeft;
        private bool _movingRight;
        private bool _jumping;

        private bool _hasDoubleJumped;
        private bool _isInvulnerable;
        private float _invulnerabilityTimer;
        private Platform _currentPlatform;

        public event EventHandler<int> LivesChanged;
        public event EventHandler<int> ScoreChanged;

        private float _jumpBufferTimer = 0; 
        private float _coyoteTimeTimer = 0; 
        private const float JUMP_BUFFER_TIME = 0.2f; 
        private const float COYOTE_TIME = 0.1f; 

        public Player(float x, float y, Image image)
            : base(x, y, 32, 48, image)
        {
            Lives = 3;
            Score = 0;
            CanDoubleJump = false;
            
            if (Sprite != null)
            {
                Sprite.BackColor = image == null ? Color.Green : Color.Transparent;
                Sprite.Tag = this; 
            }
        }

        
        public void SetInput(Keys key, bool isPressed)
        {
            switch (key)
            {
                case Keys.Left:
                case Keys.A:
                    _movingLeft = isPressed;
                    break;

                case Keys.Right:
                case Keys.D:
                    _movingRight = isPressed;
                    break;

                case Keys.Space:
                case Keys.W:
                case Keys.Up:
                    _jumping = isPressed;

                    
                    if (isPressed)
                    {
                        _jumpBufferTimer = JUMP_BUFFER_TIME; 
                    }
                    break;
            }
        }

        
        private void TryJump()
        {
            if (_jumpBufferTimer > 0)
            {
                if (IsOnGround || _coyoteTimeTimer > 0)
                {
                    
                    VelocityY = JUMP_FORCE;
                    IsOnGround = false;
                    IsJumping = true;
                    _hasDoubleJumped = false;
                    _jumpBufferTimer = 0; 

                    return; 
                }
                else if (CanDoubleJump && !_hasDoubleJumped && IsJumping)
                {
                    
                    VelocityY = JUMP_FORCE * 0.8f;
                    _hasDoubleJumped = true;
                    _jumpBufferTimer = 0; 
                }
            }
        }
        public override void Draw(Camera camera)
        {
            if (Sprite != null && !Sprite.IsDisposed)
            {
                if (camera.IsInView(Bounds))
                {
                    PointF screenPos = camera.WorldToScreen(Position);

                    Sprite.Location = new Point((int)screenPos.X, (int)screenPos.Y);

                    if (!_isInvulnerable)
                    {
                        Sprite.Visible = true;
                    }
                }
                else
                {
                    
                    Sprite.Visible = false;
                }
            }
        }

        public override void Update(GameTime gameTime, Camera camera)
        {
            
            if (_isInvulnerable)
            {
                _invulnerabilityTimer -= gameTime.DeltaTime;
                if (_invulnerabilityTimer <= 0)
                {
                    _isInvulnerable = false;
                    
                    if (Sprite != null) Sprite.Visible = true;
                }
                else
                {
                    
                    if (Sprite != null)
                    {
                        Sprite.Visible = (int)(_invulnerabilityTimer * 10) % 2 == 0;
                    }
                }
            }

            if (_jumpBufferTimer > 0)
            {
                _jumpBufferTimer -= gameTime.DeltaTime;
            }

            if (!IsOnGround && _coyoteTimeTimer > 0)
            {
                _coyoteTimeTimer -= gameTime.DeltaTime;
            }
            bool wasOnGround = IsOnGround;

            if (_movingLeft)
            {
                VelocityX = -MOVE_SPEED;
            }
            else if (_movingRight)
            {
                VelocityX = MOVE_SPEED;
            }
            else
            {
                VelocityX = 0;
            }
            
            if (!IsOnGround)
            {
                VelocityY += GRAVITY;

                if (VelocityY > MAX_FALL_SPEED)
                {
                    VelocityY = MAX_FALL_SPEED;
                }
            }

            if (IsOnGround && _currentPlatform != null && _currentPlatform is MovingPlatform movingPlatform)
            {
                Position = new PointF(
                    Position.X + movingPlatform.VelocityX,
                    _currentPlatform.Position.Y - Size.Height
                );
            }

            if (wasOnGround && !IsOnGround && !IsJumping)
            {
                _coyoteTimeTimer = COYOTE_TIME;
            }

            if (_jumpBufferTimer > 0)
            {
                TryJump();
            }

            float oldX = Position.X;
            Position = new PointF(Position.X + VelocityX, Position.Y);

            CheckHorizontalCollisions();

            float oldY = Position.Y;
            Position = new PointF(Position.X, Position.Y + VelocityY);

            CheckVerticalCollisions();

            if (!IsOnGround)
            {
                _currentPlatform = null;
            }
            Draw(camera);
        }

        private void CheckHorizontalCollisions()
        {
            foreach (var obj in GameManager.Instance.GetGameObjects())
            {
                if (obj != this && obj is Platform platform && Intersects(platform))
                {
                    RectangleF playerRect = this.Bounds;
                    RectangleF platformRect = platform.Bounds;

                    if (VelocityX > 0 && playerRect.Right > platformRect.Left &&
                        playerRect.Left < platformRect.Left)
                    {
                        Position = new PointF(platformRect.Left - Size.Width, Position.Y);
                        VelocityX = 0;
                    }
                    
                    else if (VelocityX < 0 && playerRect.Left < platformRect.Right &&
                            playerRect.Right > platformRect.Right)
                    {
                        Position = new PointF(platformRect.Right, Position.Y);
                        VelocityX = 0;
                    }
                }
            }
        }

        private void CheckVerticalCollisions()
        {
            bool wasOnGround = IsOnGround;
            IsOnGround = false; 

            foreach (var obj in GameManager.Instance.GetGameObjects())
            {
                if (obj != this && obj is Platform platform && Intersects(platform))
                {
                    RectangleF playerRect = this.Bounds;
                    RectangleF platformRect = platform.Bounds;

                    if (VelocityY >= 0 && playerRect.Bottom >= platformRect.Top &&
                        playerRect.Bottom - VelocityY <= platformRect.Top + 5 &&
                        playerRect.Right > platformRect.Left + 5 &&
                        playerRect.Left < platformRect.Right - 5)
                    {
                        Position = new PointF(Position.X, platformRect.Top - Size.Height);
                        VelocityY = 0;
                        IsOnGround = true;
                        IsJumping = false;
                        _currentPlatform = platform;
                    }
                    
                    else if (VelocityY < 0 && playerRect.Top < platformRect.Bottom &&
                            playerRect.Top > platformRect.Top &&
                            playerRect.Bottom > platformRect.Bottom)
                    {
                        Position = new PointF(Position.X, platformRect.Bottom);
                        VelocityY = 0;
                    }
                }
            }
        }

        public override void OnCollision(GameObject other)
        {
            if (other is Platform platform)
            {
                
                RectangleF playerRect = this.Bounds;
                RectangleF platformRect = platform.Bounds;

                bool isAbovePlatform = playerRect.Bottom >= platformRect.Top &&
                                      playerRect.Bottom - VelocityY <= platformRect.Top &&
                                      playerRect.Right > platformRect.Left + 5 &&
                                      playerRect.Left < platformRect.Right - 5;

                if (isAbovePlatform && VelocityY >= 0)
                {
                    Position = new PointF(Position.X, platformRect.Top - Size.Height);
                    VelocityY = 0;
                    IsOnGround = true;
                    IsJumping = false;
                    _currentPlatform = platform;

                    Console.WriteLine("Игрок приземлился на платформу");
                }
                
                else if (VelocityX > 0 && playerRect.Right > platformRect.Left &&
                        playerRect.Left < platformRect.Left &&
                        playerRect.Bottom > platformRect.Top + 5)
                {
                    Position = new PointF(platformRect.Left - Size.Width, Position.Y);
                    VelocityX = 0;
                }
                
                else if (VelocityX < 0 && playerRect.Left < platformRect.Right &&
                        playerRect.Right > platformRect.Right &&
                        playerRect.Bottom > platformRect.Top + 5)
                {
                    Position = new PointF(platformRect.Right, Position.Y);
                    VelocityX = 0;
                }
                
                else if (VelocityY < 0 && playerRect.Top < platformRect.Bottom &&
                        playerRect.Top > platformRect.Top)
                {
                    Position = new PointF(Position.X, platformRect.Bottom);
                    VelocityY = 0;
                }
            }
        }

        public void AddScore(int amount)
        {
            Score += amount;
            ScoreChanged?.Invoke(this, Score);
        }

        public void TakeDamage(bool isFall = false)
        {
            if (_isInvulnerable) return;

            Lives--;
            Console.WriteLine($"Игрок получил урон. Осталось жизней: {Lives}");

            LivesChanged?.Invoke(this, Lives);

            _isInvulnerable = true;
            _invulnerabilityTimer = 2.0f; 

            if (!isFall)
            {
                VelocityY = -8.0f;
            }
        }

        public void EnableDoubleJump()
        {
            CanDoubleJump = true;
        }

        public void Reset(float x, float y, Camera camera)
        {
            Position = new PointF(x, y);

            VelocityX = 0;
            VelocityY = 0;

            IsOnGround = false;
            IsJumping = false;
            _hasDoubleJumped = false;

            _movingLeft = false;
            _movingRight = false;
            _jumping = false;
            _isInvulnerable = true;
            _invulnerabilityTimer = 1.0f;
            _currentPlatform = null;

            Draw(camera);

            if (Sprite != null)
            {
                Sprite.Visible = true;
            }
        }

        public void ResetLives()
        {
            Lives = 3;
            LivesChanged?.Invoke(this, Lives);
        }

        public void ResetScore()
        {
            Score = 0;
            ScoreChanged?.Invoke(this, Score);
        }

        public bool IsDead()
        {
            return Lives <= 0;
        }
    }
}