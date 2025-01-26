namespace RouletteProfessionalDebunker
{
    public class Program
    {
        public const int NumTrials = 1000000;

        public const int Stake = 100;
        public const int Goal = 25;
        public const int NumSpins = 100;

        public const int NumInitialDataPoints = 15;
        public const double MinDeviationForBet = 0.4;

        public static readonly int[] Progression = { 1, 1, 2, 3, 5, 7, 11, 18 };

        public static void Main(string[] args)
        {
            // Random number generator created with no seed.
            var r = new Random();
            string dataPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            // Allows us to write data to output file.
            using (StreamWriter outputFile = new StreamWriter(Path.Combine(dataPath, "OverRouletteProfessionalData.csv")))
            {
                // Run n trials.
                for (int trial = 0; trial < NumTrials; trial++)
                {
                    // Amount of money vs amount bet for each strat
                    var money = Stake;
                    var amountBet = 0;

                    // Spins stored in list
                    var spins = new LinkedList<int>();
                    
                    // Store combinations counts (i.e. dozen 1 + dozen 2 = 3, 3 seen 4 times) in dictionaries for tracking
                    var dozensCombinationToCount = new Dictionary<int, int>();
                    var columnsCombinationToCount = new Dictionary<int, int>();

                    // Points along betting progressions for each strat
                    var dozensProgression = 0;
                    var columnsProgression = 0;

                    // Set initial combo counts to 0 for every possible combo
                    for (int i = 2; i <= 6; i++)
                    {
                        dozensCombinationToCount[i] = 0;
                        columnsCombinationToCount[i] = 0;
                    }

                    // Prepopulate our data with initial spins, no bets
                    spins.AddLast(r.Next(37));
                    var initialDataPoints = 0;
                    while (initialDataPoints < NumInitialDataPoints)
                    {
                        spins.AddLast(r.Next(37));
                        var gotDataPoint = PopulateCombinationData(spins.Last, dozensCombinationToCount, columnsCombinationToCount);
                        if (gotDataPoint) initialDataPoints++;
                    }

                    // For N spins
                    for (int spin = 0; spin < NumSpins; spin++)
                    {
                        var dozenBet = 0;
                        var columnBet = 0;

                        // Find the combination for dozens/column that is lagging most behind expectation
                        var maxDozenDeviation = double.NegativeInfinity;
                        var mostDeviatedDozenCombo = 0;
                        var maxColumnDeviation = double.NegativeInfinity;
                        var mostDeviatedColumnCombo = 0;
                        for (int c = 2; c <= 6; c++)
                        {
                            var expectedOccurences = GetExpectedOccurencesOfCombination(c, spins.Count);
                            var dozenDeviation = expectedOccurences - dozensCombinationToCount[c];
                            var columnDeviation = expectedOccurences - columnsCombinationToCount[c];

                            if (-dozenDeviation > maxDozenDeviation)
                            {
                                maxDozenDeviation = dozenDeviation;
                                mostDeviatedDozenCombo = c;
                            }

                            if (-columnDeviation > maxColumnDeviation)
                            {
                                maxColumnDeviation = columnDeviation;
                                mostDeviatedColumnCombo = c;
                            }
                        }

                        var prevSpin = GetLastNonZeroSpin(spins.Last);
                        var prevDozen = GetDozen(prevSpin);
                        var prevColumn = GetColumn(prevSpin);

                        // Check if result is sufficiently "far behind." If so, place bets.
                        var expectedDozenComboOccurence = GetExpectedOccurencesOfCombination(mostDeviatedDozenCombo, spins.Count);
                        var dozenDeviationPercentage = (expectedDozenComboOccurence - maxDozenDeviation) / expectedDozenComboOccurence;
                        if (dozenDeviationPercentage >= MinDeviationForBet)
                        {
                            var targetDozen = mostDeviatedDozenCombo - prevDozen;
                            var bet = Progression[dozensProgression];
                            if (targetDozen >= 1 && targetDozen <= 3 && money >= bet)
                            {
                                dozenBet = targetDozen;
                                money -= Progression[dozensProgression];
                                amountBet += bet;
                            }
                        }

                        var expectedColumnComboOccurence = GetExpectedOccurencesOfCombination(mostDeviatedColumnCombo, spins.Count);
                        var columnDeviationPercentage = (expectedColumnComboOccurence - maxColumnDeviation) / expectedColumnComboOccurence;
                        if (maxColumnDeviation >= MinDeviationForBet)
                        {
                            var targetColumn = mostDeviatedColumnCombo - prevColumn;
                            var bet = Progression[columnsProgression];
                            if (targetColumn >= 1 && targetColumn <= 3 && money >= bet)
                            {
                                columnBet = targetColumn;
                                money -= bet;
                                amountBet += bet;
                            }
                        }

                        // Get the spin result
                        var spinResult = r.Next(37);
                        spins.AddLast(spinResult);
                        prevSpin = GetLastNonZeroSpin(spins.Last);
                        prevDozen = GetDozen(prevSpin);
                        prevColumn = GetColumn(prevSpin);
                        var curDozen = GetDozen(spinResult);
                        var curColumn = GetColumn(spinResult);

                        // Update our tracker
                        if (spinResult != 0)
                        {
                            dozensCombinationToCount[prevDozen + curDozen]++;
                            columnsCombinationToCount[prevColumn + curColumn]++;
                        }

                        // Check if we won in the dozens
                        if (dozenBet != 0)
                        {
                            if (curDozen == dozenBet)
                            {
                                money += 3 * Progression[dozensProgression];
                                dozensProgression = 0;
                            }
                            else
                            {
                                dozensProgression++;
                            }
                        }

                        // Check if we won in the columns
                        if (columnBet != 0)
                        {
                            if (curColumn == columnBet)
                            {
                                money += 3 * Progression[columnsProgression];
                                columnsProgression = 0;
                            }
                            else
                            {
                                columnsProgression++;
                            }
                        }

                        // Quit if we've reached the end of one of our progressions
                        if (dozensProgression == Progression.Length || columnsProgression == Progression.Length) break;

                        // Check if we've hit our goal.
                        if (money - Stake >= Goal) break;

                        // Check if we can no longer make any bets (broke)
                        if (money < Progression[dozensProgression] && money < Progression[columnsProgression]) break;
                    }

                    outputFile.WriteLine($"{money}, {amountBet}");
                }
            }
        }

        public static int GetDozen(int n)
        {
            if (n >= 1 && n <= 12)
            {
                return 1;
            } else if (n >= 13 && n <= 24)
            {
                return 2;
            } else if (n >= 25 && n <= 36)
            {
                return 3;
            }
            return 0;
        }

        public static int GetColumn(int n)
        {
            if (n >= 1 && n <= 36)
            {
                if (n % 3 == 0)
                {
                    return 3;
                } else if (n % 3 == 1)
                {
                    return 1;
                } else if (n % 3 == 2)
                {
                    return 2;
                }
            }

            return 0;
        }

        public static double GetExpectedOccurencesOfCombination(int c, int numSpins)
        {
            if (c < 2 || c > 6) throw new ArgumentException("Invalid combination");

            var factor = 1.0;
            switch (c)
            {
                case 2:
                case 6:
                    factor = 1;
                    break;
                case 3:
                case 5:
                    factor = 2;
                    break;
                case 4:
                    factor = 3;
                    break;
            }

            return factor * (numSpins - 1) / 9.0;
        }

        public static int GetLastNonZeroSpin(LinkedListNode<int> curSpin)
        {
            var cur = curSpin.Previous;
            while (cur != null)
            {
                if (cur.Value != 0) return cur.Value;

                cur = cur.Previous;
            }

            return 0;
        }

        public static bool PopulateCombinationData(
            LinkedListNode<int> latestSpin,
            IDictionary<int, int> dozensCombinationToCount,
            IDictionary<int, int> columnsCombinationToCount)
        {
            var cur = latestSpin.Value;
            var prev = GetLastNonZeroSpin(latestSpin);
            if (prev == 0 || cur == 0) return false;

            var prevDozen = GetDozen(prev);
            var curDozen = GetDozen(cur);

            dozensCombinationToCount[prevDozen + curDozen]++;

            var prevColumn = GetColumn(prev);
            var curColumn = GetColumn(prev);

            columnsCombinationToCount[prevDozen + curDozen]++;

            return true;
        }
    }
}
