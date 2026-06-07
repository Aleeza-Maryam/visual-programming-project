using Microsoft.ML;
using Microsoft.ML.Data;
using System;
using System.IO;

namespace AITourismPlanner.Services
{
    public class SearchData
    {
        public string Query { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
    }

    public class SearchPrediction
    {
        public string PredictedDestination { get; set; } = string.Empty;
        public string PredictedCategory { get; set; } = string.Empty;
    }

    public class ModelTrainingService
    {
        private readonly MLContext _mlContext;
        private ITransformer? _model;

        public ModelTrainingService()
        {
            _mlContext = new MLContext();
        }

        public void TrainModel(string dataPath)
        {
            try
            {
                if (!File.Exists(dataPath))
                    return;

                var data = _mlContext.Data.LoadFromTextFile<SearchData>(dataPath, separatorChar: ',', hasHeader: true);

                var pipeline = _mlContext.Transforms.Text.FeaturizeText("QueryFeatures", nameof(SearchData.Query))
                    .Append(_mlContext.Transforms.Conversion.MapValueToKey("DestinationLabel", nameof(SearchData.Destination)))
                    .Append(_mlContext.Transforms.Conversion.MapValueToKey("CategoryLabel", nameof(SearchData.Category)))
                    .Append(_mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy())
                    .Append(_mlContext.Transforms.Conversion.MapKeyToValue("PredictedDestination", "DestinationLabel"))
                    .Append(_mlContext.Transforms.Conversion.MapKeyToValue("PredictedCategory", "CategoryLabel"));

                _model = pipeline.Fit(data);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Training Error: {ex.Message}");
            }
        }

        public SearchPrediction Predict(string query)
        {
            if (_model == null)
            {
                return new SearchPrediction { PredictedDestination = "Hunza", PredictedCategory = "Standard" };
            }

            try
            {
                var predictionEngine = _mlContext.Model.CreatePredictionEngine<SearchData, SearchPrediction>(_model);
                var prediction = predictionEngine.Predict(new SearchData { Query = query });
                return prediction;
            }
            catch
            {
                return new SearchPrediction { PredictedDestination = "Hunza", PredictedCategory = "Standard" };
            }
        }
    }
}