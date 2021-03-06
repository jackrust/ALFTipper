﻿using System;
using System.Collections.Generic;
using System.Linq;
using ArtificialNeuralNetwork.DataManagement;
using AustralianRulesFootball;
using Utilities;

namespace Tipper
{
    public abstract class AFLDataInterpreter
    {
        public int DataExpiryDays = 720;

        #region inperpretations
        public struct InterpretationSubsets
        {
            public static List<int> DefaultInterpretationSubset = new List<int> { 1, 5, 11, 19, 29 };
        }

        public struct Interpretations
        {
            //If Interpretation change network will need to change too
            public static List<List<int>> BespokeApiInterpretation = new List<List<int>>
            {
                new List<int> { 9, 13, 17 },
                new List<int> { 25, 31, 37 },
                new List<int> { 1, 3, 5 },
                new List<int> { 25, 31, 37 },
                new List<int> { 25, 31, 37 }
            };

            public static List<List<int>> BespokeLegacyInterpretation = new List<List<int>>
            {
                new List<int> {1, 8, 21},
                new List<int> {1, 8, 21},
                new List<int> {1, 8, 21},
                new List<int> {1, 8, 21},
                new List<int> {1, 8, 21}
            };


            public static List<List<int>> DefaultInterpretation = new List<List<int>>
            {
                InterpretationSubsets.DefaultInterpretationSubset,
                InterpretationSubsets.DefaultInterpretationSubset,
                InterpretationSubsets.DefaultInterpretationSubset,
                InterpretationSubsets.DefaultInterpretationSubset,
                InterpretationSubsets.DefaultInterpretationSubset,
                InterpretationSubsets.DefaultInterpretationSubset
            };
        }
        #endregion

        #region DataPoint
        public DataPoint BuildDataPoint(List<Match> history, Match m)
        {
            return BuildDataPoint(history, m, Interpretations.DefaultInterpretation);
        }

        public DataPoint BuildDataPoint(List<Match> history, Match m, List<List<int>> inputInpertretation)
        {
            var datapoint = new DataPoint
            {
                Inputs = (BuildInputs(history, m, inputInpertretation)),
                Outputs = BuildOutputs(m).ToList(),
                Reference = m.ToTuple()
            };
            return datapoint;
        }
        #endregion

        #region Inputs
        public List<double> BuildInputs(List<Match> history, Match m, List<List<int>> interpretation)
        {
            var input = new List<double>();
            
            //V1 - measure by Score

            //Scores By Team
            if (interpretation.Count < 1)
                return input;
            foreach (var term in interpretation[0])
            {
                if(term > 0)
                    input.AddRange(ExtractTeamScoreInputSet(m, history, term, ExtractInputSetForScore));
            }

            //Scores By Ground
            if (interpretation.Count < 2)
                return input;
            foreach (var term in interpretation[1])
            {
                if (term > 0)
                    input.AddRange(ExtractGroundScoreInputSet(m, history, term, ExtractInputSetForScore));
            }

            //Scores By State longerTerm
            if (interpretation.Count < 3)
                return input;
            foreach (var term in interpretation[2])
            {
                if (term > 0)
                    input.AddRange(ExtractStateScoreInputSet(m, history, term, ExtractInputSetForScore));
            }

            //Scores by Day
            if (interpretation.Count < 4)
                return input;
            foreach (var term in interpretation[3])
            {
                if (term > 0)
                    input.AddRange(ExtractDayScoreInputSet(m, history, term, ExtractInputSetForScore));
            }

            //Recent Shared Opponents
            if (interpretation.Count < 5)
                return input;
            foreach (var term in interpretation[4])
            {
                if (term > 0)
                    input.AddRange(ExtractSharedOpponentScoreSet(m, history, term, ExtractInputSetForScore));
            }

            //Scores by quality of recent Opponents
            if (interpretation.Count < 6)
                return input;
            foreach (var term in interpretation[4])
            {
                if (term > 0)
                    input.AddRange(ExtractQualityOfRecentOpponentScoreSet(m, history, term, ExtractInputSetForOppositionScore));
            }

            //Outcome focus
            //Wins By Team
            if (interpretation.Count < 1)
                return input;
            foreach (var term in interpretation[0])
            {
                if (term > 0)
                    input.AddRange(ExtractTeamWinInputSet(m, history, term, ExtractInputSetForWin));
            }
            
            return input;
        }

        private IEnumerable<double> ExtractTeamWinInputSet(Match m, List<Match> matches, int term, Func<Match, List<Match>, int, Func<Match, bool>, Func<Match, bool>, IEnumerable<double>> extrator)
        {
            Func<Match, bool> homeWherePredicate = (x => x.HasTeam(m.Home) && x.Date > m.Date.AddDays(-DataExpiryDays));
            Func<Match, bool> awayWherePredicate = (x => x.HasTeam(m.Away) && x.Date > m.Date.AddDays(-DataExpiryDays));
            return extrator(m, matches, term, homeWherePredicate, awayWherePredicate);
        }

        private IEnumerable<double> ExtractTeamScoreInputSet(Match m, List<Match> matches, int term, Func<Match, List<Match>, int, Func<Match, bool>, Func<Match, bool>, IEnumerable<double>> extrator)
        {
            Func<Match, bool> homeWherePredicate = (x => x.HasTeam(m.Home) && x.Date > m.Date.AddDays(-DataExpiryDays));
            Func<Match, bool> awayWherePredicate = (x => x.HasTeam(m.Away) && x.Date > m.Date.AddDays(-DataExpiryDays));
            return extrator(m, matches, term, homeWherePredicate, awayWherePredicate);
        }

        private IEnumerable<double> ExtractGroundScoreInputSet(Match m, List<Match> matches, int term, Func<Match, List<Match>, int, Func<Match, bool>, Func<Match, bool>, IEnumerable<double>> extrator)
        {
            const int relevantYearsDifference = -12;

            Func<Match, bool> homeWherePredicate =
                (x =>
                    x.Ground.Equals(m.Ground) && x.HasTeam(m.Home) && x.Date > m.Date.AddYears(relevantYearsDifference) && x.Date > m.Date.AddDays(-DataExpiryDays));
            Func<Match, bool> awayWherePredicate =
                (x =>
                    x.Ground.Equals(m.Ground) && x.HasTeam(m.Away) && x.Date > m.Date.AddYears(relevantYearsDifference) && x.Date > m.Date.AddDays(-DataExpiryDays));
            return extrator(m, matches, term, homeWherePredicate, awayWherePredicate);
        }

        private IEnumerable<double> ExtractStateScoreInputSet(Match m, List<Match> matches, int term, Func<Match, List<Match>, int, Func<Match, bool>, Func<Match, bool>, IEnumerable<double>> extrator)
        {
            const int relevantYearsDifference = -6;
            Func<Match, bool> homeWherePredicate =
                (x =>
                    x.Ground.State.Equals(m.Ground.State) && x.HasTeam(m.Home) &&
                    x.Date > m.Date.AddYears(relevantYearsDifference) && x.Date > m.Date.AddDays(-DataExpiryDays));
            Func<Match, bool> awayWherePredicate =
                (x =>
                    x.Ground.State.Equals(m.Ground.State) && x.HasTeam(m.Away) &&
                    x.Date > m.Date.AddYears(relevantYearsDifference) && x.Date > m.Date.AddDays(-DataExpiryDays));
            return extrator(m, matches, term, homeWherePredicate, awayWherePredicate);
        }

        private IEnumerable<double> ExtractDayScoreInputSet(Match m, List<Match> matches, int term, Func<Match, List<Match>, int, Func<Match, bool>, Func<Match, bool>, IEnumerable<double>> extrator)
        {
            Func<Match, bool> homeWherePredicate = (x => x.Date.DayOfWeek == m.Date.DayOfWeek && x.HasTeam(m.Home) && x.Date > m.Date.AddDays(-DataExpiryDays));
            Func<Match, bool> awayWherePredicate = (x => x.Date.DayOfWeek == m.Date.DayOfWeek && x.HasTeam(m.Away) && x.Date > m.Date.AddDays(-DataExpiryDays));
            return extrator(m, matches, term, homeWherePredicate, awayWherePredicate);
        }

        private IEnumerable<double> ExtractSharedOpponentScoreSet(Match m, List<Match> matches,
            int term, Func<Match, List<Match>, int, Func<Match, bool>, Func<Match, bool>, IEnumerable<double>> extrator)
        {
            const int numOpponents = 24;
            var recentMatchesHome =
                matches.Where(match => match.HasTeam(m.Home) && !match.HasTeam(m.Away))
                    .OrderByDescending(match => match.Date)
                    .Take(numOpponents)
                    .ToList();
            var recentMatchesAway =
                matches.Where(match => match.HasTeam(m.Away) && !match.HasTeam(m.Home))
                    .OrderByDescending(match => match.Date)
                    .Take(numOpponents)
                    .ToList();

            Func<Match, bool> homeWherePredicate =
                (x => x.HasTeam(m.Home) &&
                      x.HasTeam(recentMatchesAway.Select(y => y.GetOpposition(m.Away)).ToList()) &&
                      x.Date > m.Date.AddDays(-DataExpiryDays));
            Func<Match, bool> awayWherePredicate =
                (x => x.HasTeam(m.Away) && 
                      x.HasTeam(recentMatchesHome.Select(y => y.GetOpposition(m.Home)).ToList()) &&
                      x.Date > m.Date.AddDays(-DataExpiryDays));
            return extrator(m, matches, term, homeWherePredicate, awayWherePredicate);
        }

        private IEnumerable<double> ExtractQualityOfRecentOpponentScoreSet(Match m, List<Match> matches,
            int term,  Func<int,
            List<Tuple<Score, Score, DateTime>>,
            List<Tuple<Score, Score, DateTime>>, IEnumerable<double>> extractor)
        {
            const int numOpponents = 24;
            var recentHomeMatches =
                matches.Where(match => match.HasTeam(m.Home) && !match.HasTeam(m.Away))
                    .OrderByDescending(match => match.Date)
                    .Take(numOpponents)
                    .ToList();

            var recentAwayMatches =
                matches.Where(match => match.HasTeam(m.Away) && !match.HasTeam(m.Home))
                    .OrderByDescending(match => match.Date)
                    .Take(numOpponents)
                    .ToList();

            //Foreach recent game get some recent matches by that team
            var recentHomeRecentOpponentMatches = new List<Tuple<Score, Score, DateTime>>();
            foreach (var recentHomeMatch in recentHomeMatches)
            {
                var opposition = recentHomeMatch.GetOpposition(m.Home);
                foreach (var match in matches.Where(match => match.HasTeam(opposition) && !match.HasTeam(m.Home) && match.Date <= recentHomeMatch.Date.AddMonths(1) && match.Date >= recentHomeMatch.Date.AddMonths(-1)))
                {
                    var oppositionInQuestionScore = match.GetTeamScore(opposition);
                    var oppositionOfOppositionScore = match.GetOppositionScore(opposition);
                    var date = match.Date;
                    var tuple = new Tuple<Score, Score, DateTime>(oppositionInQuestionScore, oppositionOfOppositionScore, date);
                    recentHomeRecentOpponentMatches.Add(tuple);
                }
            }

            var recentAwayRecentOpponentMatches = new List<Tuple<Score, Score, DateTime>>();
            foreach (var recentAwayMatch in recentAwayMatches)
            {
                var opposition = recentAwayMatch.GetOpposition(m.Away);
                foreach (var match in matches.Where(match => match.HasTeam(opposition) && !match.HasTeam(m.Away) && match.Date <= recentAwayMatch.Date.AddMonths(1) && match.Date >= recentAwayMatch.Date.AddMonths(-1)))
                {
                    var oppositionInQuestionScore = match.GetTeamScore(opposition);
                    var oppositionOfOppositionScore = match.GetOppositionScore(opposition);
                    var date = match.Date;
                    var tuple = new Tuple<Score, Score, DateTime>(oppositionInQuestionScore, oppositionOfOppositionScore, date);
                    recentAwayRecentOpponentMatches.Add(tuple);
                }
            }

            return extractor(term, recentHomeRecentOpponentMatches, recentAwayRecentOpponentMatches);
        }

        protected abstract IEnumerable<double> ExtractInputSetForScore(Match m, List<Match> matches, int term,
            Func<Match, bool> homeWherePredicate, Func<Match, bool> awayWherePredicate);

        protected abstract IEnumerable<double> ExtractInputSetForWin(Match m, List<Match> matches, int term,
            Func<Match, bool> homeWherePredicate, Func<Match, bool> awayWherePredicate);


        protected abstract IEnumerable<double> ExtractInputSetForOppositionScore(int term,
            List<Tuple<Score, Score, DateTime>> homeOppositionScores,
            List<Tuple<Score, Score, DateTime>> awayOppositionScores);


        public static double ExtractInput(List<Match> s, Func<Match, bool> wherePredicate, int takeLength,
            Func<Match, double> sumSelector, Func<double, double> maxFunc)
        {
            var value = s
                .Where(wherePredicate)
                .OrderByDescending(x => x.Date)
                .Take(takeLength)
                .Sum(sumSelector);
            var max = maxFunc(
                s
                    .Where(wherePredicate)
                    .OrderByDescending(x => x.Date)
                    .Take(takeLength)
                    .Count());
            return Numbery.Normalise(value, max);
        }

        public static double ExtractInputFromScoreScoreDateTuple(int takeLength,
    Func<Tuple<Score, Score, DateTime>, double> sumSelector, List<Tuple<Score, Score, DateTime>> homeOppositionScores, Func<double, double> maxFunc)
        {
            var value = homeOppositionScores
                .OrderByDescending(x => x.Item3)
                .Take(takeLength)
                .Sum(sumSelector);
            var max = maxFunc(
                homeOppositionScores
                    .OrderByDescending(x => x.Item3)
                    .Take(takeLength)
                    .Count());
            return Numbery.Normalise(value, max);
        }
        #endregion

        #region Outputs
        public abstract IEnumerable<double> BuildOutputs(Match m);
        #endregion

        #region constants
        public static double GetMaxSeasonRounds(double rounds)
        {
            return rounds;
        }

        public static double GetMaxSeasonTotal(double rounds)
        {
            return Util.MaxAverage * rounds;
        }

        public static double GetMaxSeasonGoals(double rounds)
        {
            return Util.MaxGoals * rounds;
        }

        public static double GetMaxSeasonPoints(double rounds)
        {
            return Util.MaxPoints * rounds;
        }
        #endregion
    }
}
