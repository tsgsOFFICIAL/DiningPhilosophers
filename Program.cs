using System;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;

namespace DiningPhilosophers
{
    class Program
    {
        private static bool _running = true;
        private const int _philosopherCount = 5;
        private static Philosopher[] _philosophers = InitializePhilosophers(); // Initialize and return as Philosopher array
        static void Main()
        {
            // Construct philosophers and forks

            // Start dinner
            Console.WriteLine("Dinner Time!");

            // Spawn threads for each philosopher, and run their eating cycle
            Thread[] philosopherThreads = new Thread[_philosophers.Length];
            Thread uiThread = new Thread(ShowUI);
            uiThread.Start();

            byte index = 0;
            foreach (Philosopher philosopher in _philosophers)
            {
                Thread philosopherThread = new Thread(philosopher.EatAll);
                philosopherThreads[index] = philosopherThread;
                philosopherThread.Start();
                index++;
            }

            // Wait for all philosopher's to finish dining (Join blocks the main thread until the thread is completed)
            foreach (Thread thread in philosopherThreads)
            {
                thread.Join();
            }

            _running = false;

            Thread.Sleep(250);
            Console.SetCursorPosition(0, 5);
            // Done
            Console.WriteLine("Dinner is over!");
            Console.WriteLine("Bed time fellas!");
            Console.ReadKey(true);
        }

        static void ShowUI()
        {
            Console.WriteLine($" _________________\n"
                             + "|                 |\n"
                             + "| |O |O |O |O |O  |\n"
                             + " `---------------´");

            while (_running)
            {
                foreach (Philosopher philosopher in _philosophers)
                {
                    Console.SetCursorPosition(philosopher.Index + (2 * philosopher.Index) + 2, 3);
                    if (philosopher.HasLeftFork && philosopher.HasRightFork)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write("/O\\");
                    }
                    else
                    {
                        if (philosopher.HasLeftFork)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.Write("/");
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.Write("O");
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.Write("|O");
                        }
                        if (philosopher.HasRightFork)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.Write("\\ ");
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.Write("| ");
                        }
                    }
                    Console.ForegroundColor = ConsoleColor.Green;
                }
            }
            Console.SetCursorPosition(0, 3);
            Console.Write("| |O |O |O |O |O  |");
        }
        static Philosopher[] InitializePhilosophers()
        {
            // Construct philosophers
            Philosopher[] philosophers = new Philosopher[_philosopherCount];
            for (int i = 0; i < _philosopherCount; i++)
            {
                philosophers[i] = new Philosopher(philosophers, i);
            }

            // Assign forks to each philosopher
            foreach (Philosopher philosopher in philosophers)
            {
                // Assign left fork
                philosopher.LeftFork = philosopher.LeftPhilosopher.RightFork ?? new Fork();

                // Assign right fork
                philosopher.RightFork = philosopher.RightPhilosopher.LeftFork ?? new Fork();
            }

            return philosophers;
        }
    }

    public class Philosopher
    {
        // Fields
        private readonly Philosopher[] _allPhilosophers;
        private readonly int _index;
        private const int _timesToEat = 5;
        private int _timesEaten = 0;
        private string name;
        private State state;
        private Fork leftFork;
        private Fork rightFork;
        private bool hasLeftFork;
        private bool hasRightFork;

        // Properties
        public int Index
        {
            get { return _index; }
        }
        public string Name
        {
            get { return name; }
            private set { name = value; }
        }
        public State State
        {
            get { return state; }
            private set { state = value; }
        }

        public Fork LeftFork
        {
            get { return leftFork; }
            set { leftFork = value; }
        }
        public Fork RightFork
        {
            get { return rightFork; }
            set { rightFork = value; }
        }
        public bool HasLeftFork
        {
            get { return hasLeftFork; }
            set { hasLeftFork = value; }
        }
        public bool HasRightFork
        {
            get { return hasRightFork; }
            set { hasRightFork = value; }
        }
        // Constructor
        public Philosopher(Philosopher[] allPhilosophers, int index)
        {
            _allPhilosophers = allPhilosophers;
            _index = index;
            this.Name = $"Philosopher {_index}";
            this.State = State.Thinking;
        }
        #region Methods
        public Philosopher LeftPhilosopher
        {
            get
            {
                if (_index == 0)
                    return _allPhilosophers[_allPhilosophers.Length - 1];
                else
                    return _allPhilosophers[_index - 1];
            }
        }

        public Philosopher RightPhilosopher
        {
            get
            {
                if (_index == _allPhilosophers.Length - 1)
                    return _allPhilosophers[0];
                else
                    return _allPhilosophers[_index + 1];
            }
        }

        public void EatAll()
        {
            // Cycle through thinking and eating until done eating.
            while (_timesEaten < _timesToEat)
            {
                this.Think(); // Change the state to thinking
                if (this.PickUp()) // Attempt picking up both forks
                {
                    // Forks acquired, eat up
                    this.Eat();

                    // Release forks
                    this.PutDownLeft();
                    this.PutDownRight();
                }
            }
        }

        private bool PickUp()
        {
            // Try to pick up the left fork
            if (Monitor.TryEnter(this.LeftFork))
            {
                this.HasLeftFork = true;
                //DrawPhilosopher(this._index, this.hasLeftFork, this.HasRightFork);
                //DrawPhilosophers(this._allPhilosophers);
                //Console.WriteLine(this.Name + " picks up left fork.");

                // Now try to pick up the right
                if (Monitor.TryEnter(this.RightFork))
                {
                    this.HasRightFork = true;
                    //DrawPhilosopher(this._index, this.hasLeftFork, this.HasRightFork);
                    //DrawPhilosophers(this._allPhilosophers);
                    //Console.WriteLine(this.Name + " picks up right fork.");

                    // Both forks acquired, its time to eat
                    return true;
                }
                else
                {
                    // Could not get the right fork, so put down the left
                    this.PutDownLeft();
                }
            }

            // Could not acquire a fork, try again
            //DrawPhilosopher(this._index, this.hasLeftFork, this.HasRightFork);
            //DrawPhilosophers(this._allPhilosophers);
            return false;
        }

        private void Eat()
        {
            this.State = State.Eating;
            _timesEaten++;
            Random random = new Random();
            Thread.Sleep(random.Next(10, 1000 + 1));
            //Console.WriteLine(this.Name + " eats.");
        }

        private void PutDownLeft()
        {
            Monitor.Exit(this.LeftFork);
            this.HasLeftFork = false;
            //DrawPhilosopher(this._index, this.hasLeftFork, this.HasRightFork);
            //DrawPhilosophers(this._allPhilosophers);
            //Console.WriteLine(this.Name + " puts down left fork.");
        }

        private void PutDownRight()
        {
            Monitor.Exit(this.RightFork);
            this.HasRightFork = false;
            //DrawPhilosopher(this._index, this.hasLeftFork, this.HasRightFork);
            //DrawPhilosophers(this._allPhilosophers);
            //Console.WriteLine(this.Name + " puts down right fork.");
        }

        /// <summary>
        /// Draws the philosopher and his forks
        /// </summary>
        /// <param name="philPos">Philosopher position</param>
        /// <param name="leftFork">Does he have the left fork?</param>
        /// <param name="rightFork">Does he have the right fork?</param>
        static void DrawPhilosopher(int philPos, bool leftFork = false, bool rightFork = false)
        {
            switch (philPos)
            {
                case 0:
                    philPos = 3;
                    break;
                case 1:
                    philPos = 6;
                    break;
                case 2:
                    philPos = 9;
                    break;
                case 3:
                    philPos = 12;
                    break;
                case 4:
                    philPos = 15;
                    break;
            }
            // Left Fork
            Console.SetCursorPosition(philPos - 1, 3);
            if (!leftFork)
                Console.Write('|');
            else
                Console.Write('/');
            // Right Fork
            Console.SetCursorPosition(philPos + 1, 3);
            if (!rightFork)
                Console.Write('|');
            else
                Console.Write('\\');
        }

        static void DrawPhilosophers(Philosopher[] philosophers)
        {
            Console.SetCursorPosition(1, 3);
            foreach (Philosopher philosopher in philosophers)
            {
                Console.Write("O".PadLeft(3));
            }
        }

        private void Think()
        {
            this.State = State.Thinking;
        }
        #endregion
    }

    public enum State
    {
        Thinking = 0,
        Eating = 1
    }

    public class Fork
    {
        private static int _count = 1;
        public string Name { get; private set; }

        public Fork()
        {
            this.Name = "Fork " + _count++;
        }
    }
}