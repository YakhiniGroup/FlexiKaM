namespace KaplanMeierEstimator.Common
{
    public class Patient
    {
        public int Id { get; set; }

        public EventFreeSurvival CensorEvent { get; set; }

        public int CensorEventTime { get; set; }
    }
}
