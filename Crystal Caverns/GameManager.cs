using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Crystal_Caverns.Utils;

namespace Crystal_Caverns.Model
{
    public class GameManager
    {
        
        private static GameManager _instance;
        public static GameManager Instance => _instance ?? (_instance = new GameManager());

        private const int DEFAULT_LIVES = 3;

        private List<GameObject> _gameObjects;
        private Player _player;
        private Door _exitDoor;
        private List<Collectible> _collectibles;
        private List<Enemy> _enemies;
        private List<Platform> _platforms;

        private Camera _camera;
        private SizeF _worldSize = new SizeF(3000, 1500);

        private float _lastPlayerDamageTime = 0f;
        private const float PLAYER_DAMAGE_COOLDOWN = 1.0f;
        public bool IsGameRunning { get; private set; }
        public bool IsGameOver { get; private set; }
        public bool IsVictory { get; private set; }
        public int TotalCrystals => _collectibles?.Count(c => c is Collectible) ?? 0;
        public int CollectedCrystals => _collectibles?.Count(c => c.IsCollected) ?? 0;

        public event EventHandler<GameStateEventArgs> GameStateChanged;
        public event EventHandler<ScoreEventArgs> ScoreChanged;
        public event EventHandler<LivesEventArgs> LivesChanged;
        public event EventHandler<CollectiblesEventArgs> CollectiblesChanged;

        private GameManager()
        {
            _gameObjects = new List<GameObject>();
            _collectibles = new List<Collectible>();
            _enemies = new List<Enemy>();
            _platforms = new List<Platform>();
        }

        public void StartGame(Form gameForm)
        {
            if (gameForm == null) return;

            CleanupForm(gameForm);

            IsGameRunning = true;
            IsGameOver = false;
            IsVictory = false;

            _camera = new Camera(
            new SizeF(gameForm.ClientSize.Width, gameForm.ClientSize.Height),
            _worldSize);
  
            LoadLevel(gameForm);
            _camera.CalculateWorldBounds();
            if (_player != null)
            {
                LivesChanged?.Invoke(this, new LivesEventArgs(_player.Lives));
                ScoreChanged?.Invoke(this, new ScoreEventArgs(_player.Score));
                CollectiblesChanged?.Invoke(this, new CollectiblesEventArgs(CollectedCrystals, TotalCrystals));
            }

            if (_player != null)
            {
                _camera.SetTarget(_player);
            }

            GameStateChanged?.Invoke(this, new GameStateEventArgs(IsGameRunning, IsGameOver, IsVictory));
        }

        public void PauseGame()
        {
            if (IsGameRunning)
            {
                IsGameRunning = false;
                GameStateChanged?.Invoke(this, new GameStateEventArgs(IsGameRunning, IsGameOver, IsVictory));
            }
        }

        public void ResumeGame()
        {
            if (!IsGameRunning && !IsGameOver)
            {
                IsGameRunning = true;
                GameStateChanged?.Invoke(this, new GameStateEventArgs(IsGameRunning, IsGameOver, IsVictory));
            }
        }

        public void EndGame(bool victory)
        {
            IsGameRunning = false;
            IsGameOver = true;
            IsVictory = victory;

            GameStateChanged?.Invoke(this, new GameStateEventArgs(IsGameRunning, IsGameOver, IsVictory));
        }

        private void LoadLevel(Form gameForm)
        {
            _gameObjects.Clear();
            _collectibles.Clear();
            _enemies.Clear();
            _platforms.Clear();

            Image playerSprite = Utils.GameSprites.GetPlayerSprite();
            Image doorClosedSprite = Utils.GameSprites.GetDoorClosedSprite();
            Image doorOpenSprite = Utils.GameSprites.GetDoorOpenSprite();
            Image platformSprite = Utils.GameSprites.GetPlatformSprite();
            Image crystalSprite = Utils.GameSprites.GetCrystalSprite();
            Image enemySprite = Utils.GameSprites.GetEnemySprite();

            _player = new Player(100, 300, playerSprite);
            _player.LivesChanged += (sender, lives) => LivesChanged?.Invoke(this, new LivesEventArgs(lives));
            _player.ScoreChanged += (sender, score) => ScoreChanged?.Invoke(this, new ScoreEventArgs(score));
            _gameObjects.Add(_player);
            gameForm.Controls.Add(_player.Sprite);

            CreatePlatform(50, 400, 300, 20, null, gameForm);
            CreatePlatform(400, 450, 200, 20, null, gameForm);
            CreatePlatform(650, 400, 250, 20, null, gameForm);

            CreateCollectible(150, 370, 10, crystalSprite, gameForm);
            CreateCollectible(450, 420, 10, crystalSprite, gameForm);
            CreateCollectible(750, 370, 10, crystalSprite, gameForm);

            PatrolEnemy enemy1 = new PatrolEnemy(
                500, 420, _player, 2.0f, 100.0f, enemySprite
            );
            _gameObjects.Add(enemy1);
            _enemies.Add(enemy1);
            gameForm.Controls.Add(enemy1.Sprite);

            for (int i = 0; i < 8; i++)
            {
                float x = 950 + i * 200;
                float y = 400 + (float)Math.Sin(i * 0.7) * 100;

                CreatePlatform(x, y, 150, 20, null, gameForm);

                if (i % 2 == 0)
                {
                    CreateCollectible(x + 75, y - 30, 10, crystalSprite, gameForm);
                }

                if (i % 3 == 0)
                {
                    ChasingEnemy enemy = new ChasingEnemy(
                        x + 50, y - 32, _player, 3.0f, 250.0f, enemySprite
                    );
                    _gameObjects.Add(enemy);
                    _enemies.Add(enemy);
                    gameForm.Controls.Add(enemy.Sprite);
                }
            }

            for (int i = 0; i < 6; i++)
            {
                float x = 300 + (float)Math.Sin(i * 0.8) * 400;
                float y = 550 + i * 100;

                CreatePlatform(x, y, 200, 20, null, gameForm);

                CreateCollectible(x + 50, y - 30, 20, crystalSprite, gameForm);
                CreateCollectible(x + 150, y - 30, 20, crystalSprite, gameForm);

                if (i == 5)
                {
                    CreateSpecialCrystal(x + 100, y - 30, CrystalType.DoubleJump, null, gameForm);
                }
            }

            for (int i = 0; i < 10; i++)
            {
                float x = 50 + (i % 2) * 150;
                float y = 350 - i * 80;

                CreatePlatform(x, y, 120, 20, null, gameForm);

                if (i % 2 == 0)
                {
                    CreateCollectible(x + 60, y - 30, 10, crystalSprite, gameForm);
                }

                if (i == 9)
                {
                    FlyingEnemy enemy = new FlyingEnemy(
                        x + 60, y - 100, _player, 2.5f, 180.0f, 60.0f, null
                    );
                    _gameObjects.Add(enemy);
                    _enemies.Add(enemy);
                    gameForm.Controls.Add(enemy.Sprite);
                }
            }

            for (int i = 0; i < 5; i++)
            {
                float x = 2400 + i * 150;
                float y = 400 - i * 10;

                CreatePlatform(x, y, 100, 20, null, gameForm);
            }

            _exitDoor = new Door(2900, 330, null);
            _gameObjects.Add(_exitDoor);
            gameForm.Controls.Add(_exitDoor.Sprite);

            CollectiblesChanged?.Invoke(this, new CollectiblesEventArgs(CollectedCrystals, TotalCrystals));
        }

        
        private void CreatePlatform(float x, float y, float width, float height, Image image, Form gameForm)
        {
            Platform platform = new Platform(x, y, width, height, image);
            _gameObjects.Add(platform);
            _platforms.Add(platform);
            gameForm.Controls.Add(platform.Sprite);
        }

        private void CreateCollectible(float x, float y, int value, Image image, Form gameForm)
        {
            Collectible collectible = new Collectible(x, y, value, image);
            _gameObjects.Add(collectible);
            _collectibles.Add(collectible);
            gameForm.Controls.Add(collectible.Sprite); 
        }

        private void CreateSpecialCrystal(float x, float y, CrystalType type, Image image, Form gameForm)
        {
            SpecialCrystal crystal = new SpecialCrystal(x, y, type, image);
            _gameObjects.Add(crystal);
            _collectibles.Add(crystal);
            gameForm.Controls.Add(crystal.Sprite);
        }

        
        public void Update(GameTime gameTime)
        {
            if (!IsGameRunning) return;
            _lastPlayerDamageTime += gameTime.DeltaTime;
            _camera.Update(gameTime);
            
            foreach (var obj in _gameObjects.ToList())
            {
                if (obj.IsActive)
                {
                    obj.Update(gameTime, _camera);
                }
            }

            CheckCollisions();

            CheckGameState();
        }

        
        private void CheckCollisions()
        {
            
            if (_player != null && _player.IsActive)
            {
                foreach (var obj in _gameObjects)
                {
                    if (obj != _player && obj.IsActive && _player.Intersects(obj))
                    {
                        _player.OnCollision(obj);

                        if (obj is Enemy enemy)
                        {
                            if (_lastPlayerDamageTime >= PLAYER_DAMAGE_COOLDOWN)
                            {
                                _player.TakeDamage();
                                _lastPlayerDamageTime = 0f; 

                                float pushDirection = _player.Position.X < enemy.Position.X ? -1 : 1;
                                _player.VelocityX = pushDirection * 8f;
                                _player.VelocityY = -5f;
                            }
                        }


                        if (obj is Collectible collectible && !collectible.IsCollected)
                        {
                            bool intersects = _player.Intersects(obj);
                            Console.WriteLine($"Проверка кристалла: Позиция игрока={_player.Position}, " +
                                             $"Позиция кристалла={collectible.Position}, " +
                                             $"Пересечение={intersects}, " +
                                             $"Уже собран={collectible.IsCollected}");
                            collectible.Collect();
                            _player.AddScore(collectible.Value);

                            
                            if (collectible is SpecialCrystal specialCrystal)
                            {
                                ApplySpecialCrystalEffect(specialCrystal.Type);
                            }

                            
                            CollectiblesChanged?.Invoke(this,
                                new CollectiblesEventArgs(CollectedCrystals, TotalCrystals));

                            
                            if (CollectedCrystals == TotalCrystals && _exitDoor != null)
                            {
                                _exitDoor.Open();
                            }
                        }

                        if (obj is Door door && door.IsOpen)
                        {
                            EndGame(true); 
                        }
                    }
                }
            }

            
            foreach (var enemy in _enemies)
            {
                if (enemy.IsActive)
                {
                    foreach (var platform in _platforms)
                    {
                        if (platform.IsActive && enemy.Intersects(platform))
                        {
                            enemy.OnCollision(platform);
                        }
                    }
                }
            }
        }

        
        private void ApplySpecialCrystalEffect(CrystalType type)
        {
            if (_player == null) return;

            switch (type)
            {
                case CrystalType.DoubleJump:
                    _player.EnableDoubleJump();
                    break;
                case CrystalType.SpeedBoost:
                    
                    break;
                case CrystalType.Invincibility:
                    
                    break;
            }
        }

        
        private void CheckGameState()
        {
            if (_player == null) return;

            float fallDeathY = _worldSize.Height + 100; 

            if (_player.Position.Y > fallDeathY)
            {
                Console.WriteLine($"Игрок упал на Y={_player.Position.Y}, отнимаем жизнь");

                _player.TakeDamage();

                PointF spawnPoint = FindSafeSpawnPoint();

                _player.Reset(spawnPoint.X, spawnPoint.Y, _camera);

                Console.WriteLine($"Игрок респавнится на X={spawnPoint.X}, Y={spawnPoint.Y}");
            }

            
            if (_player.IsDead())
            {
                EndGame(false); 
            }
        }
        private PointF FindSafeSpawnPoint()
        {
            PointF defaultSpawn = new PointF(100, 300);
  
            Platform safePlatform = null;

            foreach (var obj in _platforms)
            {
                if (obj.Position.X < 300 && obj.Position.Y > 100 && obj.Position.Y < 500)
                {
                    safePlatform = obj;
                    break;
                }
            }

            if (safePlatform == null && _platforms.Count > 0)
            {
                safePlatform = _platforms[0];
            }

            if (safePlatform != null)
            {
                return new PointF(
                    safePlatform.Position.X + safePlatform.Size.Width / 2 - _player.Size.Width / 2,
                    safePlatform.Position.Y - _player.Size.Height - 5
                );
            }

            return defaultSpawn;
        }

        
        public void CleanupForm(Form form)
        {
            if (form == null) return;

            List<Control> controlsToRemove = new List<Control>();
            foreach (Control control in form.Controls)
            {
                if (control is PictureBox)
                {
                    controlsToRemove.Add(control);
                }
            }

            foreach (Control control in controlsToRemove)
            {
                form.Controls.Remove(control);
                control.Dispose();
            }

            
            foreach (var obj in _gameObjects)
            {
                obj.Dispose();
            }

            
            _gameObjects.Clear();
            _collectibles.Clear();
            _enemies.Clear();
            _platforms.Clear();

            
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        
        public void RestartGame(Form gameForm)
        {
            PauseGame();

            CleanupForm(gameForm);

            StartGame(gameForm);

            if (_camera != null)
            {
                _camera.RecalculateBounds();
            }

            if (_player != null)
            {
                LivesChanged?.Invoke(this, new LivesEventArgs(_player.Lives));

                ScoreChanged?.Invoke(this, new ScoreEventArgs(_player.Score));
                CollectiblesChanged?.Invoke(this, new CollectiblesEventArgs(CollectedCrystals, TotalCrystals));
            }

            if (_camera != null && _player != null)
            {
                
                float centerX = _player.Position.X + (_player.Size.Width / 2) - (_camera.ViewportSize.Width / 2);
                float centerY = _player.Position.Y + (_player.Size.Height / 2) - (_camera.ViewportSize.Height / 2);
                
                typeof(Camera).GetProperty("Position").SetValue(_camera, new PointF(centerX, centerY));
            }
        }

        
        public Player GetPlayer() => _player;

        public IReadOnlyList<GameObject> GetGameObjects() => _gameObjects.AsReadOnly();

        public IReadOnlyList<Collectible> GetCollectibles() => _collectibles.AsReadOnly();

        public IReadOnlyList<Enemy> GetEnemies() => _enemies.AsReadOnly();

        public IReadOnlyList<Platform> GetPlatforms() => _platforms.AsReadOnly();
    }

    
    public class GameStateEventArgs : EventArgs
    {
        public bool IsRunning { get; }
        public bool IsOver { get; }
        public bool IsVictory { get; }

        public GameStateEventArgs(bool isRunning, bool isOver, bool isVictory)
        {
            IsRunning = isRunning;
            IsOver = isOver;
            IsVictory = isVictory;
        }
    }

    public class ScoreEventArgs : EventArgs
    {
        public int Score { get; }

        public ScoreEventArgs(int score)
        {
            Score = score;
        }
    }

    public class LivesEventArgs : EventArgs
    {
        public int Lives { get; }

        public LivesEventArgs(int lives)
        {
            Lives = lives;
        }
    }

    public class CollectiblesEventArgs : EventArgs
    {
        public int Collected { get; }
        public int Total { get; }

        public CollectiblesEventArgs(int collected, int total)
        {
            Collected = collected;
            Total = total;
        }
    }
}