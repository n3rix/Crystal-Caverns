// Создай новый файл NavigationGraph.cs в папке Utils
using System;
using System.Collections.Generic;
using System.Drawing;
using Crystal_Caverns.Model;

namespace Crystal_Caverns.Utils
{
    public class NavigationGraph
    {
        private class Node
        {
            public PointF Position { get; }
            public List<Node> Connections { get; }
            public Platform Platform { get; }

            public Node(PointF position, Platform platform)
            {
                Position = position;
                Connections = new List<Node>();
                Platform = platform;
            }
        }

        private List<Node> _nodes = new List<Node>();
        private const float MAX_HORIZONTAL_DISTANCE = 150f; // Максимальное расстояние для горизонтального прыжка
        private const float MAX_JUMP_HEIGHT = 150f; // Максимальная высота прыжка
        private const float JUMP_DISTANCE = 100f; // Расстояние, на которое может прыгнуть

        public NavigationGraph(IEnumerable<Platform> platforms)
        {
            // Создаем узлы для каждой платформы
            foreach (var platform in platforms)
            {
                // Создаем узлы на левом и правом краях платформы
                float leftX = platform.Position.X + 10;
                float rightX = platform.Position.X + platform.Size.Width - 10;
                float y = platform.Position.Y - 2; // Чуть выше платформы

                Node leftNode = new Node(new PointF(leftX, y), platform);
                Node rightNode = new Node(new PointF(rightX, y), platform);

                _nodes.Add(leftNode);
                _nodes.Add(rightNode);
            }

            // Связываем узлы между собой
            ConnectNodes();
        }

        private void ConnectNodes()
        {
            // Для каждого узла ищем доступные соединения
            foreach (var nodeA in _nodes)
            {
                foreach (var nodeB in _nodes)
                {
                    if (nodeA == nodeB) continue;

                    // Проверяем, можно ли соединить узлы
                    float dx = Math.Abs(nodeA.Position.X - nodeB.Position.X);
                    float dy = nodeA.Position.Y - nodeB.Position.Y;

                    // Соединяем узлы на одной платформе
                    if (nodeA.Platform == nodeB.Platform)
                    {
                        nodeA.Connections.Add(nodeB);
                        continue;
                    }

                    // Соединяем узлы для горизонтального прыжка
                    if (dx <= MAX_HORIZONTAL_DISTANCE && Math.Abs(dy) < 20) // Увеличиваем допуск по высоте
                    {
                        nodeA.Connections.Add(nodeB);
                        continue;
                    }

                    // УЛУЧШАЕМ ПРЫЖКИ ВВЕРХ: соединяем узлы для прыжка вверх с большим расстоянием
                    if (dy < 0 && Math.Abs(dy) <= MAX_JUMP_HEIGHT && dx <= JUMP_DISTANCE * 1.5f) // Увеличиваем дистанцию
                    {
                        // Добавляем вертикальные соединения с большим приоритетом
                        nodeA.Connections.Add(nodeB); // Добавляем связь для прыжка вверх
                        nodeA.Connections.Add(nodeB); // Добавляем тот же узел второй раз для увеличения его веса
                        continue;
                    }

                    // Соединяем узлы для падения вниз - с низким приоритетом
                    if (dy > 0 && dx <= JUMP_DISTANCE)
                    {
                        if (dy < 50) // Только маленькие падения добавляем напрямую
                        {
                            nodeA.Connections.Add(nodeB);
                        }
                        // Для больших падений не добавляем связь, чтобы враг искал другой путь
                    }
                }
            }
        }

        // Находим ближайший узел к заданной позиции
        private Node FindClosestNode(PointF position)
        {
            Node closest = null;
            float minDistance = float.MaxValue;

            foreach (var node in _nodes)
            {
                float distance = CalculateDistance(position, node.Position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closest = node;
                }
            }

            return closest;
        }

        // Поиск пути от начальной позиции к целевой
        public List<PointF> FindPath(PointF start, PointF target)
        {
            Node startNode = FindClosestNode(start);
            Node targetNode = FindClosestNode(target);

            if (startNode == null || targetNode == null)
                return new List<PointF>();

            // Используем A* алгоритм для поиска пути
            var openSet = new List<Node> { startNode };
            var closedSet = new List<Node>();

            var gScore = new Dictionary<Node, float>();
            var fScore = new Dictionary<Node, float>();
            var cameFrom = new Dictionary<Node, Node>();

            foreach (var node in _nodes)
            {
                gScore[node] = float.MaxValue;
                fScore[node] = float.MaxValue;
            }

            gScore[startNode] = 0;
            fScore[startNode] = CalculateDistance(startNode.Position, targetNode.Position);

            while (openSet.Count > 0)
            {
                Node current = GetNodeWithLowestFScore(openSet, fScore);

                if (current == targetNode)
                {
                    return ReconstructPath(cameFrom, current);
                }

                openSet.Remove(current);
                closedSet.Add(current);

                foreach (var neighbor in current.Connections)
                {
                    if (closedSet.Contains(neighbor))
                        continue;

                    float tentativeGScore = gScore[current] + CalculateDistance(current.Position, neighbor.Position);

                    if (!openSet.Contains(neighbor))
                        openSet.Add(neighbor);
                    else if (tentativeGScore >= gScore[neighbor])
                        continue;

                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeGScore;
                    fScore[neighbor] = gScore[neighbor] + CalculateDistance(neighbor.Position, targetNode.Position);
                }
            }

            return new List<PointF>();
        }

        private Node GetNodeWithLowestFScore(List<Node> openSet, Dictionary<Node, float> fScore)
        {
            Node lowest = openSet[0];
            float lowestScore = fScore[lowest];

            foreach (var node in openSet)
            {
                if (fScore[node] < lowestScore)
                {
                    lowest = node;
                    lowestScore = fScore[node];
                }
            }

            return lowest;
        }

        private List<PointF> ReconstructPath(Dictionary<Node, Node> cameFrom, Node current)
        {
            var path = new List<PointF> { current.Position };

            while (cameFrom.ContainsKey(current))
            {
                current = cameFrom[current];
                path.Insert(0, current.Position);
            }

            return path;
        }

        private float CalculateDistance(PointF a, PointF b)
        {
            float dx = a.X - b.X;
            float dy = a.Y - b.Y;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }
    }
}