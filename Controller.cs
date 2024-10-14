using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;

namespace coursework
{
    internal class Controller
    {
        public Settings actSett;
        private double penaltyMult;
        private double delta, epsilonForGradient, epsilonForPenalty;


        public Controller()
        {
            actSett = new Settings();
            penaltyMult = 0;
            delta = 0.01;
            epsilonForGradient = 0.1;
            epsilonForPenalty = 0.1;
        }

        public double CalculateTotalCost(List<double> point)
        {
            double n = point[0];
            double t1 = point[1];
            double t2 = point[2];
            double t3 = point[3];


            double permCost = (actSett.PackingCost * n * t1 * t2) +
                              (2 * actSett.PackingCost * n * t1 * t3);

            double varSurface = (n * t1 * t2) + (2 * n * t2 * t3) - actSett.FreePackagingAmount;

            double varCost = actSett.PackingCost * Math.Max(0.0, varSurface);

            double transCost = actSett.TransportationCost * n;

            double totalCost = permCost + varCost + transCost;

            return totalCost;
        }

        public double CalculatePenalty(List<double> point)
        {
            double n = point[0];
            double t1 = point[1];
            double t2 = point[2];
            double t3 = point[3];

            double p1 = actSett.Volume - (n * t1 * t2 * t3);
            double pen1 = Math.Pow(Math.Max(0.0, p1), 3);

            double p2 = t1 - actSett.MaxLength;
            double pen2 = Math.Pow(Math.Max(0.0, p2), 3);

            double p3 = t2 - actSett.MaxWidth;
            double pen3 = Math.Pow(Math.Max(0.0, p3), 3);

            double p4 = t3 - actSett.MaxHeight;
            double pen4 = Math.Pow(Math.Max(0.0, p4), 3);

            double p5 = 1 - n;
            double pen5 = Math.Pow(Math.Max(0.0, p5), 3);

            double p6 = -t1;
            double pen6 = Math.Pow(Math.Max(0.0, p6), 3);

            double p7 = -t2;
            double pen7 = Math.Pow(Math.Max(0.0, p7), 3);

            double p8 = -t3;
            double pen8 = Math.Pow(Math.Max(0.0, p8), 3);

            double penalty = penaltyMult * (pen1 + pen2 + pen3 + pen4 + pen5 + pen6 + pen7 + pen8);

            return penalty;
        }

        public double CalculateTotalCostWithPenalty(List<double> point)
        {
            return CalculateTotalCost(point) + CalculatePenalty(point);
        }

        public List<double> PenaltyMethod(List<double> startPoint)
        {
            penaltyMult = 1;
            var borderDecreaseRate = 10;

            var currentPoint = startPoint;

            while (true)
            {
                var newPoint = GradientDescent(currentPoint);

                var penalty = CalculatePenalty(newPoint);

                if (penalty < epsilonForPenalty)
                {
                    newPoint = FormatResult(newPoint);
                    return newPoint;
                }
                else
                {
                    penaltyMult *= borderDecreaseRate;
                    currentPoint = newPoint;
                }
            }
        }

        public List<double> GradientDescent(List<double> startPoint)
        {
            double alpha = 1;
            var alphaDecreaseRate = 0.9;
            var currentPoint = startPoint;
            while (true)
            {
                var currentValue = CalculateTotalCostWithPenalty(currentPoint);
                var newPoint = new List<double>();
                for (var i = 0; i < currentPoint.Count; i++)
                {
                    Func<double, double> func = x =>
                        CalculateTotalCostWithPenalty(CopyPointWithReplace(currentPoint, x, i));
                    newPoint.Add(currentPoint[i] -
                        alpha * (1.0 / Convert.ToDouble(startPoint.Count)) * GetDerivative(func, currentPoint[i]));
                }
                var newValue = CalculateTotalCostWithPenalty(newPoint);

                if (newValue > currentValue)
                    alpha *= alphaDecreaseRate;
                else
                {
                    if (currentValue - newValue <= epsilonForGradient)
                        return newPoint;
                    else
                        currentPoint = newPoint;
                }
            }
        }

        private double GetDerivative(Func<double, double> function, double point)
        {
            return (function(point + delta) - function(point - delta)) /
               (2 * delta);
        }

        private List<double> CopyPointWithReplace(List<double> point, double replace, int replaceIndex)
        {
            var result = new List<double>();
            for (var i = 0; i < point.Count; i++)
                if (i == replaceIndex)
                    result.Add(replace);
                else
                    result.Add(point[i]);

            return result;
        }

        private void CalculateDeltas(Settings sett)
        {

            if (sett.Probability > 0 && sett.Probability < 1)
            {
                // volume
                sett.VolumeDelta = (float)Math.Round(
                    Math.Sqrt((1 - sett.Probability) / sett.Probability),
                    3);

                // transportation cost
                sett.TransportationCostDelta = (float)Math.Round(
                    Math.Sqrt(2 * Math.Log(1 / sett.Probability)),
                    3);

                // packing cost
                sett.PackingCostDelta = (float)Math.Round(
                    Math.Sqrt((2 - (2 * sett.Probability)) / sett.Probability),
                    3);

                // Pessimistic settings
                sett.Pess = new Settings();

                sett.Pess.Volume = sett.Volume * (1 + sett.VolumeDelta);
                sett.Pess.PackingCost = sett.PackingCost * (1 + sett.PackingCostDelta);
                sett.Pess.TransportationCost = sett.TransportationCost * (1 + sett.TransportationCostDelta);
                sett.Pess.FreePackagingAmount = sett.FreePackagingAmount;
                sett.Pess.MaxLength = sett.MaxLength;
                sett.Pess.MaxWidth = sett.MaxWidth;
                sett.Pess.MaxHeight = sett.MaxHeight;

                // Optimistic settings
                sett.Opt = new Settings();

                sett.Opt.Volume = sett.Volume * (1 - sett.VolumeDelta);
                sett.Opt.PackingCost = sett.PackingCost * (1 - sett.PackingCostDelta);
                sett.Opt.TransportationCost = sett.TransportationCost * (1 - sett.TransportationCostDelta);
                sett.Opt.FreePackagingAmount = sett.FreePackagingAmount;
                sett.Opt.MaxLength = sett.MaxLength;
                sett.Opt.MaxWidth = sett.MaxWidth;
                sett.Opt.MaxHeight = sett.MaxHeight;

            }
        }

        public Settings ReadSettings()
        {
            Settings settings = new Settings();

            using (StreamReader reader = File.OpenText(@".\settings.json"))
            {
                JObject obj = (JObject)JToken.ReadFrom(new JsonTextReader(reader));

                settings.Volume = (float)obj.GetValue("volume");
                settings.PackingCost = (float)obj.GetValue("packingCost");
                settings.FreePackagingAmount = (float)obj.GetValue("freePackagingAmount");
                settings.TransportationCost = (float)obj.GetValue("transportationCost");
                settings.Probability = (float)obj.GetValue("probability");
                settings.MaxLength = (float)obj.GetValue("maxLength");
                settings.MaxWidth = (float)obj.GetValue("maxWidth");
                settings.MaxHeight = (float)obj.GetValue("maxHeight");
            }

            CalculateDeltas(settings);

            return settings;
        }

        private List<double> FormatResult(List<double> result)
        {
            return new List<double>
            {
                Math.Ceiling(result[0]),
                Math.Round(result[1], 3),
                Math.Round(result[2], 3),
                Math.Round(result[3], 3),
            };
        }
    }
}
