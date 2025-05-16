using System;
using System.Windows.Forms;

namespace Crystal_Caverns.Controller
{
    public class GameController
    {
        
        private readonly Model.GameManager _gameManager;
 
        private Timer _gameTimer;
 
        private Model.GameTime _gameTime;
        private DateTime _lastUpdateTime;

        
        public GameController()
        {
            _gameManager = Model.GameManager.Instance;
            _gameTime = new Model.GameTime();
            _lastUpdateTime = DateTime.Now;

            _gameTimer = new Timer
            {
                Interval = 16 
            };
            _gameTimer.Tick += GameTimer_Tick;
        }

        
        private void GameTimer_Tick(object sender, EventArgs e)
        {
            DateTime currentTime = DateTime.Now;
            TimeSpan deltaTimeSpan = currentTime - _lastUpdateTime;
            _lastUpdateTime = currentTime;

            _gameTime.DeltaTime = (float)deltaTimeSpan.TotalSeconds;
            _gameTime.TotalTime += _gameTime.DeltaTime;

            _gameManager.Update(_gameTime);
        }
 
        public void StartGame(Form gameForm)
        {
            _gameManager.StartGame(gameForm);
            StartGameLoop();
        }

        public void StartGameLoop()
        {
            _lastUpdateTime = DateTime.Now;
            _gameTimer.Start();
        }
        
        public void StopGameLoop()
        {
            _gameTimer.Stop();
        }

        public void PauseGame()
        {
            _gameManager.PauseGame();
        }

        public void ResumeGame()
        {
            _gameManager.ResumeGame();
        }

        public void RestartGame(Form gameForm)
        {
            _gameTimer.Stop();
 
            _gameManager.RestartGame(gameForm);
            
            var player = _gameManager.GetPlayer();
            if (player != null)
            {
                
                Keys[] allKeys = { Keys.Left, Keys.Right, Keys.Up, Keys.Down,
                           Keys.A, Keys.D, Keys.W, Keys.S, Keys.Space };

                foreach (var key in allKeys)
                {
                    player.SetInput(key, false);
                }
            }

            
            if (gameForm != null)
            {
                gameForm.Invalidate();
                gameForm.Update();
                
                gameForm.Focus();
                
                gameForm.BeginInvoke(new Action(() =>
                {
                    
                    System.Threading.Thread.Sleep(50);
                    gameForm.Focus();
                }));
            }

            
            _lastUpdateTime = DateTime.Now;
            _gameTimer.Start();
        }

        
        public void HandleKeyDown(Keys key)
        {   
            var player = _gameManager.GetPlayer();
            if (player != null)
            {
                player.SetInput(key, true);
            }

            switch (key)
            {
                case Keys.Escape:
                    if (_gameManager.IsGameRunning)
                        PauseGame();
                    else
                        ResumeGame();
                    break;
                case Keys.R:
                    
                    
                    break;
            }
        }

        public void HandleKeyUp(Keys key)
        {
            var player = _gameManager.GetPlayer();

            if (player != null)
            {
                player.SetInput(key, false);
            }
        }

        public void Dispose()
        {
            _gameTimer?.Stop();
            _gameTimer?.Dispose();
        }
    }
}