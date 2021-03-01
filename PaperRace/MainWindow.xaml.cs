﻿using PaperRace.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PaperRace
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Текущий сдвиг карты по осям
        /// </summary>
        int _deltaX, _deltaY = 0;

        /// <summary>
        /// Текущая скорость машины по осям
        /// </summary>
        int _currentSpeedX, _currentSpeedY = 0;

        /// <summary>
        /// Ототбражение машины на карте
        /// </summary>
        Image _carImage { get; set; }

        /// <summary>
        /// Список отрезков пройденного пути
        /// </summary>
        List<PathElement> _pathList = new List<PathElement>();

        /// <summary>
        /// Точки на карте
        /// </summary>
        List<Button> _mapPoints = new List<Button>();

        /// <summary>
        /// Список фрагментов дороги
        /// </summary>
        List<RoadElement> _roadElements = new List<RoadElement>();

        /// <summary>
        /// Список нарисованных на карте объектов
        /// </summary>
        List<Shape> _roadObjects = new List<Shape>();

        /// <summary>
        /// Список объектов для красоты снаружи дороги
        /// </summary>
        List<Image> _offRoadObjects = new List<Image>();

        /// <summary>
        /// Словарь сопоставления полей карты и типа данного поля
        /// </summary>
        Dictionary<Button, FieldTypeEnum> _fieldsDictionary = new Dictionary<Button, FieldTypeEnum>();
        public MainWindow()
        {
            InitializeComponent();

            NewGameStart();
        }

        /// <summary>
        /// Нарисовать пройденный путь
        /// </summary>
        private void ShowPaths()
        {
            foreach (var path in _pathList)
            {
                Line line = new Line
                {
                    X1 = path.FromX + (_deltaX - path.DeltaX),
                    Y1 = path.FromY + (_deltaY - path.DeltaY),
                    X2 = path.ToX + (_deltaX - path.DeltaX),
                    Y2 = path.ToY + (_deltaY - path.DeltaY),
                    Stroke = Brushes.Blue,
                    StrokeThickness = 4,
                    Margin = new Thickness(5, 5, 0, 0)
                };
                Panel.SetZIndex(line, 7);
                Map.Children.Add(line);
                _roadObjects.Add(line);
            }
        }

        /// <summary>
        /// Генерация сетки на карте
        /// </summary>
        private void GenerateWeb()
        {
            // Земля на карте = слой 0
            var earth = new Image()
            {
                Width = 1000,
                Height = 1000,
                Source = new BitmapImage(new Uri("/Images/earth.jpg", UriKind.Relative))
            };
            Panel.SetZIndex(earth, 0);
            Map.Children.Add(earth);


            foreach (var item in _offRoadObjects)
            {
                Map.Children.Remove(item);
            }
            _offRoadObjects.Clear();

            if (_mapPoints.Count == 0)
            {
                for (int i = 5; i < 780; i += 20)
                {
                    Rectangle rect = new Rectangle
                    {
                        Width = 1,
                        Height = 700,
                        Fill = Brushes.Red,
                        Opacity = 0.3
                    };

                    Map.Children.Add(rect);
                    Canvas.SetTop(rect, 0);
                    Canvas.SetLeft(rect, i);
                }

                for (int i = 5; i < 760; i += 20)
                {
                    Rectangle rect = new Rectangle
                    {
                        Width = 760,
                        Height = 1,
                        Fill = Brushes.Red,
                        Opacity = 0.3
                    };

                    Map.Children.Add(rect);
                    Canvas.SetTop(rect, i);
                    Canvas.SetLeft(rect, 0);
                }

                for (int i = 0; i < 780; i += 20)
                {
                    for (int j = 0; j < 780; j += 20)
                    {
                        var button = new Button
                        {
                            Width = 10,
                            Height = 10,
                        };
                        button.Click += DoStep;

                        _fieldsDictionary[button] = FieldTypeEnum.OffRoadField;

                        if (Math.Abs(GameSettings.UserPositionX + _currentSpeedX * 20 - i) <= 20 && Math.Abs(GameSettings.UserPositionY + _currentSpeedY * 20 - j) <= 20)
                        {
                            // Проверим будет ли данная точка вне дороги и пометим другим цветом
                            var isNewPointOnRoad = IsPointOnRoad(new Point(i, j));

                            button.Opacity = 1.0;

                            if (isNewPointOnRoad)
                            {
                                button.Background = Brushes.LightGreen;
                                _fieldsDictionary[button] = FieldTypeEnum.SafeStep;
                            }
                            else
                            {
                                button.Background = Brushes.Red;
                                _fieldsDictionary[button] = FieldTypeEnum.OffRoadStep;
                            }

                            if (GameSettings.UserPositionX == i && GameSettings.UserPositionY == j)
                            {
                                button.Background = Brushes.Black;
                                _fieldsDictionary[button] = FieldTypeEnum.UserPosition;
                            }
                            //else
                            //{
                            //    button.Background = Brushes.LightGreen;
                            //}
                            button.IsEnabled = true;
                        }
                        else
                        {
                            // Проверим будет ли данная точка вне дороги и пометим другим цветом
                            var isNewPointOnRoad = IsPointOnRoad(new Point(i, j));

                            if (isNewPointOnRoad)
                            {
                                _fieldsDictionary[button] = FieldTypeEnum.RoadField;
                                button.Opacity = 0.3;
                                button.Background = Brushes.MediumAquamarine;
                                button.IsEnabled = true;
                            }
                            else
                            {
                                button.Opacity = 0.3;
                                _fieldsDictionary[button] = FieldTypeEnum.OffRoadField;
                                button.Background = Brushes.White;
                                button.IsEnabled = false;

                                var random = new Random();
                                var elkaChance = random.Next(0, 101);
                                if (elkaChance < 20)
                                {
                                    var elka = new Image()
                                    {
                                        Width = 40,
                                        Height = 40,
                                        Source = new BitmapImage(new Uri("/Images/elka.png", UriKind.Relative))
                                    };
                                    Panel.SetZIndex(elka, 100);
                                    Map.Children.Add(elka);
                                    _offRoadObjects.Add(elka);
                                    Canvas.SetTop(elka, j - 15);
                                    Canvas.SetLeft(elka, i - 15);
                                }
                            }
                        }

                        _mapPoints.Add(button);

                        Panel.SetZIndex(button, 10);
                        Map.Children.Add(button);
                        Canvas.SetTop(button, j);
                        Canvas.SetLeft(button, i);
                    }
                }
            }
            else
            {
                foreach (var button in _mapPoints)
                {
                    var x = Canvas.GetLeft(button);
                    var y = Canvas.GetTop(button);

                    if (Math.Abs(GameSettings.UserPositionX + _currentSpeedX * 20 - x) <= 20 && Math.Abs(GameSettings.UserPositionY + _currentSpeedY * 20 - y) <= 20)
                    {
                        // Проверим будет ли данная точка вне дороги и пометим другим цветом
                        var isNewPointOnRoad = IsPointOnRoad(new Point(x, y));

                        button.Opacity = 1.0;

                        if (isNewPointOnRoad)
                        {
                            button.Background = Brushes.LightGreen;
                            _fieldsDictionary[button] = FieldTypeEnum.SafeStep;
                        }
                        else
                        {
                            button.Background = Brushes.Red;
                            _fieldsDictionary[button] = FieldTypeEnum.OffRoadStep;
                        }

                        Panel.SetZIndex(button, 100);
                        button.IsEnabled = true;
                    }
                    else
                    {
                        // Проверим будет ли данная точка вне дороги и пометим другим цветом
                        var isNewPointOnRoad = IsPointOnRoad(new Point(x, y));

                        if (isNewPointOnRoad)
                        {
                            button.Opacity = 0.3;
                            _fieldsDictionary[button] = FieldTypeEnum.RoadField;
                            button.Background = Brushes.MediumAquamarine;
                            button.IsEnabled = true;
                        }
                        else
                        {
                            button.Opacity = 0.3;
                            _fieldsDictionary[button] = FieldTypeEnum.OffRoadField;
                            button.Background = Brushes.White;
                            button.IsEnabled = false;

                            var random = new Random();
                            var elkaChance = random.Next(0, 101);
                            if (elkaChance < 20)
                            {
                                var elka = new Image()
                                {
                                    Width = 40,
                                    Height = 40,
                                    Source = new BitmapImage(new Uri("/Images/elka.png", UriKind.Relative))
                                };
                                Panel.SetZIndex(elka, 8);
                                Map.Children.Add(elka);
                                _offRoadObjects.Add(elka);
                                Canvas.SetTop(elka, y - 15);
                                Canvas.SetLeft(elka, x - 15);
                            }
                        }

                        Panel.SetZIndex(button, 2);
                    }

                    if (x == GameSettings.UserPositionX && y == GameSettings.UserPositionY)
                    {
                        _fieldsDictionary[button] = FieldTypeEnum.UserPosition;
                        button.Background = Brushes.Black;
                        Panel.SetZIndex(button, 10);
                        button.IsEnabled = true;
                    }
                }
            }
        }

        /// <summary>
        /// Текущая линейная скорость
        /// </summary>
        private int CurrentSpeed
        {
            get
            {
                var speedX = Math.Abs(_currentSpeedX);
                var speedY = Math.Abs(_currentSpeedY);

                if (speedX > speedY) return speedX;
                return speedY;
            }
        }

        /// <summary>
        /// Выполнить действие на карте. Обычно - новый ход.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DoStep(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var x = Convert.ToInt32(Canvas.GetLeft(button));
            var y = Convert.ToInt32(Canvas.GetTop(button));

            if (_fieldsDictionary[button] == FieldTypeEnum.UserPosition)
            {
                MessageBox.Show("Car options !");
                return;
            }
            else if (_fieldsDictionary[button] == FieldTypeEnum.RoadField || _fieldsDictionary[button] == FieldTypeEnum.OffRoadField)
            {

            }
            else
            {
                _currentSpeedX = Convert.ToInt32((x - GameSettings.UserPositionX) / 20);
                _currentSpeedY = Convert.ToInt32((y - GameSettings.UserPositionY) / 20);

                var path = new PathElement
                {
                    FromX = GameSettings.UserPositionX,
                    FromY = GameSettings.UserPositionY,
                    ToX = x,
                    ToY = y,
                    DeltaX = _deltaX,
                    DeltaY = _deltaY
                };
                _pathList.Add(path);

                // Проверка хода: не вышли ли с дороги
                var isNewPointOnRoad = IsPointOnRoad(new Point(x, y));
                if (!isNewPointOnRoad)
                {
                    MessageBox.Show("Конец игры !");
                    NewGameStart();
                    return;
                }

                _deltaX -= (x - GameSettings.UserPositionX);
                _deltaY -= (y - GameSettings.UserPositionY);


                
                GenerateWeb();
                ShowRoad();
                ShowPaths();
                SetCarAttitude();
                ShowCarMoveArea();
                RefreshInfo();
            }
        }

        /// <summary>
        /// Обновление информации на карте
        /// </summary>
        private void RefreshInfo()
        {
            SpeedTb.Text = CurrentSpeed.ToString();
        }

        private bool IsPointOnRoad(Point p)
        {
            p.X -= _deltaX;
            p.Y -= _deltaY;

            foreach (var roadElement in _roadElements)
            {
                // Сначала проверим, не лежит ли точка в круге с центром в конце фрагмента дороги 

                if (Math.Pow(p.X - roadElement.EndPoint.X, 2) + Math.Pow(p.Y - roadElement.EndPoint.Y, 2) <= Math.Pow(GameSettings.RoadWidth / 2, 2)) return true;

                // Площадь треугольника
                var Sabc = 0.5 * Math.Abs((roadElement.StartPoint.X - p.X) * (roadElement.EndPoint.Y - p.Y) - (roadElement.EndPoint.X - p.X) * (roadElement.StartPoint.Y - p.Y));
                // Длина стороны AC
                var AC = Math.Sqrt(Math.Pow((roadElement.StartPoint.X - roadElement.EndPoint.X), 2) + Math.Pow((roadElement.StartPoint.Y - roadElement.EndPoint.Y), 2));
                // Длина стороны AB
                var AB = Math.Sqrt(Math.Pow((roadElement.StartPoint.X - p.X), 2) + Math.Pow((roadElement.StartPoint.Y - p.Y), 2));
                // Длина стороны BC
                var BC = Math.Sqrt(Math.Pow((p.X - roadElement.EndPoint.X), 2) + Math.Pow((p.Y - roadElement.EndPoint.Y), 2));
                // Длина Высоты, опущенной на сторону AC
                var h = 2 * Sabc / AC;

                // если высота больше чем ширина фрагмента дороги, то точно установлено: точка не лежит на прямоугольнике
                if (h > GameSettings.RoadWidth / 2) continue;

                // остается случай когда Высота не падает на сторону AC, а падает на продолжение стороны AC
                var angleBAC = Math.Acos((Math.Pow(AC, 2) + Math.Pow(AB, 2) - Math.Pow(BC, 2)) / (2 * AC * AB)) * 180 / Math.PI;
                var angleBCA = Math.Acos((Math.Pow(AC, 2) + Math.Pow(BC, 2) - Math.Pow(AB, 2)) / (2 * AC * BC)) * 180 / Math.PI;

                if (angleBAC > 90 || angleBCA > 90) continue;

                return true;
            }


            return false;
        }

        /// <summary>
        /// Установить правильный поворот изображения машины на карте
        /// </summary>
        private void SetCarAttitude()
        {
            var lastPath = _pathList.Last();
            var newAngle = lastPath.Angle;

            RotateTransform rotate = _carImage.RenderTransform as RotateTransform;
            if (rotate == null)
            {
                rotate = new RotateTransform(0, 22, 22);
                _carImage.RenderTransform = rotate;
            }
            rotate.Angle = newAngle;
        }

        /// <summary>
        /// Клик в меню создание новой игры
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NewGame_Click(object sender, RoutedEventArgs e)
        {
            NewGameStart();
        }

        /// <summary>
        /// Сбросить настройки для начала новой игры
        /// </summary>
        private void ResetGameProcess()
        {
            _currentSpeedX = _currentSpeedY = _deltaX = _deltaY = 0;
            _pathList.Clear();
            _mapPoints.Clear();
            _roadElements.Clear();
            _roadObjects.Clear();

            Map.Children.RemoveRange(0, Map.Children.Count);

            RefreshInfo();
        }

        /// <summary>
        /// Начало новой игры
        /// </summary>
        private void NewGameStart()
        {
            ResetGameProcess();

            double x = GameSettings.UserPositionX;
            double y = GameSettings.UserPositionY;

            for (int i = 0; i < GameSettings.RoadElementsCount; i++)
            {
                double angle = _roadElements.Count == 0 ? 0 : _roadElements.Last().Angle;

                var roadElement = GetRoadElement(new Point(x, y), angle);
                _roadElements.Add(roadElement);

                x = roadElement.EndPoint.X;
                y = roadElement.EndPoint.Y;
            }

            GenerateWeb();
            ShowRoad();
            ShowCar();
            ShowCarMoveArea();
        }

        /// <summary>
        /// Эта область выделяет текущие возможные для хода поля
        /// </summary>
        private void ShowCarMoveArea()
        {
            var square = new Rectangle()
            {
                Width = 60,
                Height = 60,
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection() { 4, 2 },
                Stroke = Brushes.Black
            };
            Panel.SetZIndex(square, 10);
            Map.Children.Add(square);
            _roadObjects.Add(square);
            Canvas.SetTop(square, GameSettings.UserPositionY + _currentSpeedY * 20 - 25);
            Canvas.SetLeft(square, GameSettings.UserPositionX + _currentSpeedX * 20 - 25);
        }

        /// <summary>
        /// Перерисовать дорогу
        /// </summary>
        private void ShowRoad()
        {
            if (_roadObjects != null)
            {
                foreach (var roadObject in _roadObjects)
                {
                    //   var currShape = (Shape)roadObject;
                    Map.Children.Remove(roadObject);
                }
            }

            _roadObjects = new List<Shape>();

            #region Фрагменты дороги

            for (int i = 0; i < _roadElements.Count; i++)
            {
                var currElement = _roadElements[i];

                Line line = new Line
                {
                    X1 = currElement.StartPoint.X + _deltaX,
                    Y1 = currElement.StartPoint.Y + _deltaY,
                    X2 = currElement.EndPoint.X + _deltaX,
                    Y2 = currElement.EndPoint.Y + _deltaY,
                    Stroke = GameBrushes.RoadBrush,
                    StrokeDashArray = new DoubleCollection() { 4, 2 },
                    StrokeThickness = GameSettings.RoadWidth,
                    Margin = new Thickness(5, 5, 0, 0)
                };

                Panel.SetZIndex(line, 1);
                Map.Children.Add(line);
                _roadObjects.Add(line);

                double top = currElement.EndPoint.Y - GameSettings.RoadWidth / 2 + _deltaY;
                double left = currElement.EndPoint.X - GameSettings.RoadWidth / 2 + _deltaX;
                var ellipse = new Ellipse
                {
                    Width = GameSettings.RoadWidth,
                    Height = GameSettings.RoadWidth,
                    Fill = GameBrushes.RoadBrush,
                    Margin = new Thickness(5, 5, 0, 0)
                };
                Panel.SetZIndex(ellipse, 1);
                Map.Children.Add(ellipse);
                Canvas.SetTop(ellipse, top);
                Canvas.SetLeft(ellipse, left);
                _roadObjects.Add(ellipse);

            }

            #endregion

            #region Дорожная разметка

            var polyLinePointCollection = new PointCollection();
            foreach (var roadElement in _roadElements)
            {
                if (polyLinePointCollection.Count == 0) polyLinePointCollection.Add(new Point(roadElement.StartPoint.X + 5 + _deltaX, roadElement.StartPoint.Y + _deltaY));
                polyLinePointCollection.Add(new Point(roadElement.EndPoint.X + 5 + _deltaX, roadElement.EndPoint.Y + _deltaY));
            }

            var polyLine = new Polyline()
            {
                Points = polyLinePointCollection,
                Stroke = Brushes.White,
                StrokeDashArray = new DoubleCollection() { 6, 4 },
                StrokeThickness = 2
            };
            Panel.SetZIndex(polyLine, 12);
            Map.Children.Add(polyLine);
            _roadObjects.Add(polyLine);

            #endregion

            #region Дорожные знаки

            for (int i = 1; i < _roadElements.Count; i++)
            {
                var prevElement = _roadElements[i - 1];
                var currElement = _roadElements[i];

                // Опасный поворот
                if (Math.Abs(currElement.Angle - prevElement.Angle) > 70 && Math.Abs(currElement.Angle - prevElement.Angle) < 290)
                {
                    var sign = new Image()
                    {
                        Width = 40,
                        Height = 40,
                        Source = new BitmapImage(new Uri("/Images/danger.png", UriKind.Relative))
                    };
                    Panel.SetZIndex(sign, 20);
                    Map.Children.Add(sign);
                    _offRoadObjects.Add(sign);
                    Canvas.SetTop(sign, currElement.StartPoint.Y + _deltaY);
                    Canvas.SetLeft(sign, currElement.StartPoint.X + _deltaX);
                }
            }

            #endregion
        }

        /// <summary>
        /// Показать машину на карте
        /// </summary>
        private void ShowCar()
        {
            _carImage = new Image
            {
                Width = 44,
                Height = 44,
                Source = new BitmapImage(new Uri("/Images/car.png", UriKind.Relative))
            };
            Panel.SetZIndex(_carImage, 8);
            Map.Children.Add(_carImage);
            Canvas.SetTop(_carImage, GameSettings.UserPositionY - 15);
            Canvas.SetLeft(_carImage, GameSettings.UserPositionX - 17);
        }

        /// <summary>
        /// Генерация нового элемента дороги
        /// </summary>
        /// <param name="start"></param>
        /// <returns></returns>
        private RoadElement GetRoadElement(Point start, double currentAngle)
        {
            var roadElement = new RoadElement();

            var random = new Random();
            var newAngle = _roadElements.Count == 0 ? 0 : random.Next(GameSettings.MinAngle, GameSettings.MaxAngle);

            roadElement.Angle = newAngle + currentAngle;
            roadElement.Width = GameSettings.RoadWidth;
            roadElement.Height = random.Next(GameSettings.MinRoadLength, GameSettings.MaxRoadLength);
            roadElement.StartPoint = new Point(start.X, start.Y);

            return roadElement;
        }

    }
}
