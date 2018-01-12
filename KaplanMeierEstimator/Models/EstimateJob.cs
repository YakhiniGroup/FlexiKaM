using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace KaplanMeierEstimator.Models
{
    public class EstimateJob
    {
        [DataType(DataType.Upload)]
        public HttpPostedFileBase PatientsList { get; set; }

        [DataType(DataType.Upload)]
        public HttpPostedFileBase GenesList { get; set; }

        public string PercentStrategy { get; set; }

        public int Percent { get; set; }

    }
}