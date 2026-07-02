using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using VIVE.OpenXR;
using VIVE.OpenXR.EyeTracker;
using VIVE.OpenXR.FacialTracking;
using sxr_internal;
using VIVE.OpenXR.Samples.FacialTracking;
using VIVE.OpenXR.Samples.EyeTracker;
using SoundManager;

namespace RXT
{
    public enum DataField
    {
        SubjectID,
        Date,
        LocalTime,
        UnityTime,
        Phase,
        Trial,
        Step,
        TrialTimePassed
    }
    public enum EyeTrackingEnum
    {
        gazeOriginX,
        gazeOriginY,
        gazeOriginZ,
        gazeDirectionX,
        gazeDirectionY,
        gazeDirectionZ,
        leftPupil,
        rightPupil,
        combinedPupil,
        baselineCorrected,
        hitX,
        hitY,
        hitZ,
        objectName
    }
    [Serializable]
    public class DataPoint
    {
        public bool Toggle = true;
        public string Header;
        public DataField Field;
    }
    [Serializable]
    public class EyeTrackingDataPoints
    {
        public bool Toggle = true;
        public string Header;
        public EyeTrackingEnum Field;
    }

    [System.Serializable]
    public class EyeExpressionDataPoints
    {
        public bool Toggle = true;
        public string Header;
        public XrEyeExpressionHTC EyeExprEnums;
        
    }
    [System.Serializable]
    public class FacialExpressionDataPoints
    {
        public bool Toggle = true;
        public string Header;
        public XrLipExpressionHTC FacialExprEnums;
        
    }
    [System.Serializable]
    class SummaryValue
    {
        public double Sum;
        public int Count;

        public void Add(double value)
        {
            Sum += value;
            Count++;
        }

        public double Average => Count == 0 ? 0 : Sum / Count;
    }

    public class DataCollection : MonoBehaviour
    {
        [ContextMenu("Populate Basic Data")]
        private void PopulateBasicData()
        {
            dataPoint.Clear();

            foreach (DataField field in Enum.GetValues(typeof(DataField)))
            {
                dataPoint.Add(new DataPoint
                {
                    Toggle = true,
                    Header = field.ToString(),
                    Field = field
                });
            }
        }

        [ContextMenu("Populate Eye Tracking")]
        private void PopulateEyeTracking()
        {
            eyeTrackingDataPoints.Clear();

            foreach (EyeTrackingEnum field in Enum.GetValues(typeof(EyeTrackingEnum)))
            {
                eyeTrackingDataPoints.Add(new EyeTrackingDataPoints
                {
                    Toggle = true,
                    Header = field.ToString(),
                    Field = field
                });
            }
        }
        public static DataCollection Instance;

        [ContextMenu("Populate Eye Expressions")]
        private void PopulateEyeExpressions()
        {
            eyeExpressionDataPoints.Clear();

            foreach (var expression in EyeExprEnums)
            {
                eyeExpressionDataPoints.Add(new EyeExpressionDataPoints
                {
                    Toggle = true,
                    Header = expression.ToString(),
                    EyeExprEnums = expression
                });
            }
        }

        [ContextMenu("Populate Facial Expressions")]
        private void PopulateFacialExpressions()
        {
            facialExpressionDataPoints.Clear();

            foreach (var expression in LipExprEnums)
            {
                facialExpressionDataPoints.Add(new FacialExpressionDataPoints
                {
                    Toggle = true,
                    Header = expression.ToString(),
                    FacialExprEnums = expression
                });
            }
        }

        [SerializeField] private string SubjectID = "Participant";
        [SerializeField] private string DownloadPath = "/sdcard2";
        [SerializeField] private string BackupDownloadPath = "/sdcard";
        [SerializeField] private bool GenerateTrialSummaryFile;
        [SerializeField] private bool GenerateEventSummaryFile;
        [SerializeField] private bool EnablePupilBaselineCorrections;
        [SerializeField] private float BaselineCaptureSeconds = 1f;



        public List<DataPoint> dataPoint = new();
        public List<EyeTrackingDataPoints> eyeTrackingDataPoints = new();
        public List<EyeExpressionDataPoints> eyeExpressionDataPoints = new();
        public List<FacialExpressionDataPoints> facialExpressionDataPoints = new();
        private Dictionary<string, SummaryValue> summary = new();
        private Dictionary<string, SummaryValue> eventSummary = new();
        [SerializeField] private bool SimulateEyeTracking = false;
        private StreamWriter summaryWriter;
        private string summaryFilePath; 

        private StreamWriter eventWriter;
        private string eventFilePath; 

        private float simulatedBaseline = 3.5f;
        private float simulatedAmplitude = 0.25f;
        private float simulatedNoise = 0.03f;


        private Camera vrCamera;
        private float flushTimer = 0f;

        private readonly StringBuilder writeBuffer = new StringBuilder(1024 * 64);
        private StreamWriter writer;

        private bool headerPrinted = false;
        private bool isCollectingEventData = false;

        private string filePath;

        private Vector3 combinedGazeOrigin;
        private Vector3 combinedGazeDirection;

        private Ray gazeRay;
        private RaycastHit hit;

        private List<float> BaselinePupilStorage  = new List<float>();
        private List<float> TempBaselinePupilStorage  = new List<float>();

        private float leftPupilSize  = 0;
        private float rightPupilSize = 0;

        private float baseline           = 0f;
        private bool  baselineValid      = false;
        private bool  baselineInProgress = false;
        private bool hasHit;
        private Vector3 hitPoint;
        private string hitObjectName = "";
        private int previousValue = -1;
        private Coroutine eventCoroutine;
        


        private static readonly XrEyeExpressionHTC[] EyeExprEnums =
        {
            XrEyeExpressionHTC.XR_EYE_EXPRESSION_LEFT_BLINK_HTC,
            XrEyeExpressionHTC.XR_EYE_EXPRESSION_LEFT_WIDE_HTC,
            XrEyeExpressionHTC.XR_EYE_EXPRESSION_RIGHT_BLINK_HTC,
            XrEyeExpressionHTC.XR_EYE_EXPRESSION_RIGHT_WIDE_HTC,
            XrEyeExpressionHTC.XR_EYE_EXPRESSION_LEFT_SQUEEZE_HTC,
            XrEyeExpressionHTC.XR_EYE_EXPRESSION_RIGHT_SQUEEZE_HTC,
            XrEyeExpressionHTC.XR_EYE_EXPRESSION_LEFT_DOWN_HTC,
            XrEyeExpressionHTC.XR_EYE_EXPRESSION_RIGHT_DOWN_HTC,
            XrEyeExpressionHTC.XR_EYE_EXPRESSION_LEFT_OUT_HTC,
            XrEyeExpressionHTC.XR_EYE_EXPRESSION_RIGHT_IN_HTC,
            XrEyeExpressionHTC.XR_EYE_EXPRESSION_LEFT_IN_HTC,
            XrEyeExpressionHTC.XR_EYE_EXPRESSION_RIGHT_OUT_HTC,
            XrEyeExpressionHTC.XR_EYE_EXPRESSION_LEFT_UP_HTC,
            XrEyeExpressionHTC.XR_EYE_EXPRESSION_RIGHT_UP_HTC,
        };

        private static readonly XrLipExpressionHTC[] LipExprEnums =
        {
            XrLipExpressionHTC.XR_LIP_EXPRESSION_JAW_RIGHT_HTC,
            XrLipExpressionHTC.XR_LIP_EXPRESSION_JAW_LEFT_HTC,
            XrLipExpressionHTC.XR_LIP_EXPRESSION_JAW_FORWARD_HTC,
            XrLipExpressionHTC.XR_LIP_EXPRESSION_JAW_OPEN_HTC,
            XrLipExpressionHTC.XR_LIP_EXPRESSION_MOUTH_APE_SHAPE_HTC,
            XrLipExpressionHTC.XR_LIP_EXPRESSION_MOUTH_UPPER_RIGHT_HTC,
            XrLipExpressionHTC.XR_LIP_EXPRESSION_MOUTH_UPPER_LEFT_HTC,
            XrLipExpressionHTC.XR_LIP_EXPRESSION_MOUTH_LOWER_RIGHT_HTC,
            XrLipExpressionHTC.XR_LIP_EXPRESSION_MOUTH_LOWER_LEFT_HTC,
            XrLipExpressionHTC.XR_LIP_EXPRESSION_MOUTH_UPPER_OVERTURN_HTC,
            XrLipExpressionHTC.XR_LIP_EXPRESSION_MOUTH_LOWER_OVERTURN_HTC,
            XrLipExpressionHTC.XR_LIP_EXPRESSION_MOUTH_POUT_HTC,
            XrLipExpressionHTC.XR_LIP_EXPRESSION_MOUTH_RAISER_RIGHT_HTC,
            XrLipExpressionHTC.XR_LIP_EXPRESSION_MOUTH_RAISER_LEFT_HTC,
            XrLipExpressionHTC.XR_LIP_EXPRESSION_MOUTH_STRETCHER_RIGHT_HTC,
            XrLipExpressionHTC.XR_LIP_EXPRESSION_MOUTH_STRETCHER_LEFT_HTC,
            XrLipExpressionHTC.XR_LIP_EXPRESSION_CHEEK_PUFF_RIGHT_HTC,
            XrLipExpressionHTC.XR_LIP_EXPRESSION_CHEEK_PUFF_LEFT_HTC,
            XrLipExpressionHTC.XR_LIP_EXPRESSION_CHEEK_SUCK_HTC,
            XrLipExpressionHTC.XR_LIP_EXPRESSION_MOUTH_UPPER_UPRIGHT_HTC,
            XrLipExpressionHTC.XR_LIP_EXPRESSION_MOUTH_UPPER_UPLEFT_HTC,
            XrLipExpressionHTC.XR_LIP_EXPRESSION_MOUTH_LOWER_DOWNRIGHT_HTC,
            XrLipExpressionHTC.XR_LIP_EXPRESSION_MOUTH_LOWER_DOWNLEFT_HTC,
            XrLipExpressionHTC.XR_LIP_EXPRESSION_MOUTH_UPPER_INSIDE_HTC,
            XrLipExpressionHTC.XR_LIP_EXPRESSION_MOUTH_LOWER_INSIDE_HTC,
            XrLipExpressionHTC.XR_LIP_EXPRESSION_MOUTH_LOWER_OVERLAY_HTC,
            XrLipExpressionHTC.XR_LIP_EXPRESSION_TONGUE_LONGSTEP1_HTC,
            XrLipExpressionHTC.XR_LIP_EXPRESSION_TONGUE_LEFT_HTC,
            XrLipExpressionHTC.XR_LIP_EXPRESSION_TONGUE_RIGHT_HTC,
            XrLipExpressionHTC.XR_LIP_EXPRESSION_TONGUE_UP_HTC,
            XrLipExpressionHTC.XR_LIP_EXPRESSION_TONGUE_DOWN_HTC,
            XrLipExpressionHTC.XR_LIP_EXPRESSION_TONGUE_ROLL_HTC,
            XrLipExpressionHTC.XR_LIP_EXPRESSION_TONGUE_LONGSTEP2_HTC,
            XrLipExpressionHTC.XR_LIP_EXPRESSION_TONGUE_UPRIGHT_MORPH_HTC,
            XrLipExpressionHTC.XR_LIP_EXPRESSION_TONGUE_UPLEFT_MORPH_HTC,
            XrLipExpressionHTC.XR_LIP_EXPRESSION_TONGUE_DOWNRIGHT_MORPH_HTC,
            XrLipExpressionHTC.XR_LIP_EXPRESSION_TONGUE_DOWNLEFT_MORPH_HTC,
        };

        private string GetDataPointValue(DataField field)
        {
            return field switch
            {
                DataField.SubjectID => sxr.GetSubjectID(),
                DataField.Date => DateTime.Today.ToString("M_d"),
                DataField.LocalTime => DateTime.Now.ToString("H_m_s"),
                DataField.UnityTime => Time.time.ToString("F4"),
                DataField.Phase => sxr.GetPhase().ToString(),
                DataField.Trial => sxr.GetTrial().ToString(),
                DataField.Step => sxr.GetStepInTrial().ToString(),
                DataField.TrialTimePassed => sxr.TimePassed().ToString(),
                _ => ""
            };
        }

        private string GetEyeTrackingValue(EyeTrackingEnum field)
        {
            return field switch
            {
                EyeTrackingEnum.gazeOriginX => combinedGazeOrigin.x.ToString("F4"),
                EyeTrackingEnum.gazeOriginY => combinedGazeOrigin.y.ToString("F4"),
                EyeTrackingEnum.gazeOriginZ => combinedGazeOrigin.z.ToString("F4"),

                EyeTrackingEnum.gazeDirectionX => combinedGazeDirection.x.ToString("F4"),
                EyeTrackingEnum.gazeDirectionY => combinedGazeDirection.y.ToString("F4"),
                EyeTrackingEnum.gazeDirectionZ => combinedGazeDirection.z.ToString("F4"),

                EyeTrackingEnum.leftPupil => leftPupilSize.ToString(),
                EyeTrackingEnum.rightPupil => rightPupilSize.ToString(),

                EyeTrackingEnum.combinedPupil => CombinedPupil()?.ToString() ?? "",

                EyeTrackingEnum.baselineCorrected =>
                    (CombinedPupil() is float cp
                        ? BaselineCorrected(cp)?.ToString()
                        : "") ?? "",

                EyeTrackingEnum.hitX => hasHit ? hitPoint.x.ToString("F4") : "",
                EyeTrackingEnum.hitY => hasHit ? hitPoint.y.ToString("F4") : "",
                EyeTrackingEnum.hitZ => hasHit ? hitPoint.z.ToString("F4") : "",
                EyeTrackingEnum.objectName => hasHit ? hitObjectName : "",

                _ => ""
            };
        }

        private string GetEyeExpressionValue(XrEyeExpressionHTC expression)
        {
            return FacialTrackingData
                .EyeExpression(expression)
                .ToString();
        }

        private string GetFacialExpressionValue(XrLipExpressionHTC expression)
        {
            return FacialTrackingData
                .LipExpression(expression)
                .ToString();
        }

        // -------- INIT --------
        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject.transform.root);
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            string storageRoot = DownloadPath;
            try
            {
                string testPath = Path.Combine(storageRoot, "Download", "Experiments");
                if (!Directory.Exists(testPath))
                    Directory.CreateDirectory(testPath);
            }
            catch
            {
                storageRoot = BackupDownloadPath;
            }

            string experimentsRoot = Path.Combine(storageRoot, "Download", "Experiments");
            if (!Directory.Exists(experimentsRoot))
                Directory.CreateDirectory(experimentsRoot);

            string folderName = SubjectID;

            string subfolderBase = folderName.ToLowerInvariant();

            string subfolderName = subfolderBase;
            string subfolderPath = Path.Combine(experimentsRoot, subfolderName);

            int suffix = 1;
            while (Directory.Exists(subfolderPath))
            {
                subfolderName = $"{subfolderBase}({suffix})";
                subfolderPath = Path.Combine(experimentsRoot, subfolderName);
                suffix++;
            }

            Directory.CreateDirectory(subfolderPath);

            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            filePath = Path.Combine(subfolderPath, $"eyetracker_{timestamp}.csv");

            writer = new StreamWriter(filePath, append: false, encoding: Encoding.UTF8, bufferSize: 65536);
            writer.AutoFlush = false;

            if(GenerateTrialSummaryFile){
            summaryFilePath = Path.Combine(subfolderPath, "TrialSummary.csv");

            summaryWriter = new StreamWriter(summaryFilePath, false, Encoding.UTF8);
            summaryWriter.AutoFlush = true;

            Debug.Log("Saving to: " + filePath);
            }

            if(GenerateEventSummaryFile){
            eventFilePath = Path.Combine(subfolderPath, "EventSummary.csv");

            eventWriter = new StreamWriter(eventFilePath, false, Encoding.UTF8);
            eventWriter.AutoFlush = true;

            Debug.Log("Saving to: " + filePath);
            }

        }

        void Start()
        {
            vrCamera = Camera.main;
            sxr.SetSubjectID(SubjectID);
            StartRecording();
        }

        void WriteHeader()
        {
            string headerConstructor = "";
            foreach(var dp in dataPoint)
            {
                if (!dp.Toggle)
                {
                    continue;
                }
                headerConstructor += dp.Header + ",";
                if(GenerateTrialSummaryFile) summary[dp.Header] = new SummaryValue();
                if(GenerateEventSummaryFile) eventSummary[dp.Header] = new SummaryValue();

            }

            foreach(var dp in eyeTrackingDataPoints)
            {
                if (!dp.Toggle)
                {
                    continue;
                }
                headerConstructor += dp.Header + ",";
                if(GenerateTrialSummaryFile) summary[dp.Header] = new SummaryValue();
                if(GenerateEventSummaryFile) eventSummary[dp.Header] = new SummaryValue();
            }

            foreach(var dp in eyeExpressionDataPoints)
            {
                if (!dp.Toggle)
                {
                    continue;
                }
                headerConstructor += dp.Header + ",";
                if(GenerateTrialSummaryFile) summary[dp.Header] = new SummaryValue();
                if(GenerateEventSummaryFile) eventSummary[dp.Header] = new SummaryValue();
            }

            foreach(var dp in facialExpressionDataPoints)
            {
                if (!dp.Toggle)
                {
                    continue;
                }
                headerConstructor += dp.Header + ",";
                if(GenerateTrialSummaryFile) summary[dp.Header] = new SummaryValue();
                if(GenerateEventSummaryFile) eventSummary[dp.Header] = new SummaryValue();
            }

            writer.WriteLine(headerConstructor);
            if(GenerateTrialSummaryFile) summaryWriter.WriteLine(headerConstructor);
            if(GenerateEventSummaryFile) eventWriter.WriteLine(headerConstructor);

            headerPrinted = true;
        }

        void FlushToFile()
        {
            if (writeBuffer.Length == 0) return;
            writer.Write(writeBuffer);
            writer.Flush();
            writeBuffer.Clear();
        }

        void AppendDataPoints(StringBuilder sb)
        {
            foreach(var dp in dataPoint)
            {
                if (!dp.Toggle)
                {
                    continue;
                }
                string value = GetDataPointValue(dp.Field);

                if (double.TryParse(value, out double number))
                {
                    if (GenerateTrialSummaryFile)
                    {
                        summary[dp.Header].Add(number);
                    }

                    if(GenerateEventSummaryFile && isCollectingEventData)
                    {
                        eventSummary[dp.Header].Add(number);
                    }
                }

                sb.Append(value);
                sb.Append(',');

            }
        }

        void AppendEyeTracking(StringBuilder sb)
        {
            foreach (var dp in eyeTrackingDataPoints)
            {
                if (!dp.Toggle)
                    continue;

                string value = GetEyeTrackingValue(dp.Field);
                if (dp.Field == EyeTrackingEnum.baselineCorrected)
                {
                    float? corrected = CombinedPupil() is float cp
                        ? BaselineCorrected(cp)
                        : null;

                    if (corrected.HasValue)
                    {
                        value = corrected.Value.ToString();
                    }
                }

                if (double.TryParse(value, out double number))
                {
                    if (GenerateTrialSummaryFile)
                    {
                        summary[dp.Header].Add(number);
                    }

                    if(GenerateEventSummaryFile && isCollectingEventData)
                    {
                        eventSummary[dp.Header].Add(number);
                    }
                }

                sb.Append(value);
                sb.Append(',');
            }
        }

        void AppendFacialData(StringBuilder sb)
        {
            foreach(var dp in eyeExpressionDataPoints)
            {
                if (!dp.Toggle)
                    continue;

                string value = GetEyeExpressionValue(dp.EyeExprEnums);

                if (double.TryParse(value, out double number))
                {
                    if (GenerateTrialSummaryFile)
                    {
                        summary[dp.Header].Add(number);
                    }

                    if(GenerateEventSummaryFile && isCollectingEventData)
                    {
                        eventSummary[dp.Header].Add(number);
                    }
                }

                sb.Append(value);
                sb.Append(',');
            }

            foreach(var dp in facialExpressionDataPoints)
            {
                if (!dp.Toggle)
                    continue;

                string value = GetFacialExpressionValue(dp.FacialExprEnums);

                if (double.TryParse(value, out double number))
                {
                    if (GenerateTrialSummaryFile)
                    {
                        summary[dp.Header].Add(number);
                    }

                    if(GenerateEventSummaryFile && isCollectingEventData)
                    {
                        eventSummary[dp.Header].Add(number);
                    }
                }

                sb.Append(value);
                sb.Append(',');
            }
        }

        public void StartRecording()
        {
            if (!headerPrinted) WriteHeader();
        }

        public void PauseRecording()
        {
            FlushToFile();
        }

        void Update()
        {
            UpdateGaze();
            UpdatePupil();
            AppendDataRow();

            if (EnablePupilBaselineCorrections)
            {
                CheckForChangeInTrial();
            }

            flushTimer += Time.deltaTime;
            if (flushTimer >= 5f)
            {
                FlushToFile();
                flushTimer = 0f;
            }
        }

        void AppendDataRow()
        {

            AppendDataPoints(writeBuffer);
            AppendEyeTracking(writeBuffer);
            AppendFacialData(writeBuffer);
            
            writeBuffer.Append('\n');
        }

        void UpdateGaze()
        {
            XR_HTC_eye_tracker.Interop.GetEyeGazeData(out XrSingleEyeGazeDataHTC[] gazes);

            if(gazes == null || gazes.Length < 2)
            {
                hasHit = false;
                return;
            }

            var left  = gazes[(int)XrEyePositionHTC.XR_EYE_POSITION_LEFT_HTC];
            var right = gazes[(int)XrEyePositionHTC.XR_EYE_POSITION_RIGHT_HTC];

            if (!left.isValid && !right.isValid)
            {
                hasHit = false;
                return;
            }

            Vector3 leftPos  = left.gazePose.position.ToUnityVector();
            Vector3 rightPos = right.gazePose.position.ToUnityVector();

            Vector3 leftDir  = left.gazePose.orientation.ToUnityQuaternion() * Vector3.forward;
            Vector3 rightDir = right.gazePose.orientation.ToUnityQuaternion() * Vector3.forward;

            combinedGazeOrigin = (leftPos + rightPos) / 2f;
            combinedGazeDirection = (leftDir + rightDir).normalized;

            if (combinedGazeDirection == Vector3.zero)
            {
                hasHit = false;
                return;
            }

            Vector3 worldOrigin = vrCamera.transform.TransformPoint(combinedGazeOrigin);
            Vector3 worldDirection = vrCamera.transform.TransformDirection(combinedGazeDirection);

            gazeRay = new Ray(worldOrigin, worldDirection);

            hasHit = Physics.Raycast(gazeRay, out hit);

            if (hasHit)
            {
                hitPoint = hit.point;
                hitObjectName = hit.collider.gameObject.name;
            }
            else
            {
                hitPoint = Vector3.zero;
                hitObjectName = "";
            }
        }

        void UpdatePupil()
        {
            if (SimulateEyeTracking)
            {
                float noiseL = UnityEngine.Random.Range(-simulatedNoise, simulatedNoise);
                float noiseR = UnityEngine.Random.Range(-simulatedNoise, simulatedNoise);

                float wave = Mathf.Sin(Time.time * 2f) * simulatedAmplitude;

                leftPupilSize = simulatedBaseline + wave + noiseL;
                rightPupilSize = simulatedBaseline + wave + noiseR;


                if (baselineInProgress)
                {
                    BaselinePupilStorage.Add(leftPupilSize);
                    BaselinePupilStorage.Add(rightPupilSize);
                }

                return;
            }

            XR_HTC_eye_tracker.Interop.GetEyePupilData(out XrSingleEyePupilDataHTC[] pupils);

            if (pupils == null || pupils.Length < 2)
                return;

            var left  = pupils[(int)XrEyePositionHTC.XR_EYE_POSITION_LEFT_HTC];
            var right = pupils[(int)XrEyePositionHTC.XR_EYE_POSITION_RIGHT_HTC];

            leftPupilSize  = left.isDiameterValid  ? left.pupilDiameter  : -1;
            rightPupilSize = right.isDiameterValid ? right.pupilDiameter : -1;

            if (baselineInProgress)
            {
                if (leftPupilSize  > 0) BaselinePupilStorage.Add(leftPupilSize);
                if (rightPupilSize > 0) BaselinePupilStorage.Add(rightPupilSize); 
            }

        }

        float? CombinedPupil()
        {
            if (leftPupilSize > 0 && rightPupilSize > 0)
                return (leftPupilSize + rightPupilSize) / 2f;
            if (leftPupilSize  > 0) return leftPupilSize;
            if (rightPupilSize > 0) return rightPupilSize;
            return null;
        }

        void CheckForChangeInTrial()
        {
            if(previousValue != sxr.GetTrial())
            {
                if(GenerateTrialSummaryFile) WriteTrialSummary();
                previousValue = sxr.GetTrial();
                StartBaseline();
            }
        }

        public void StartBaseline()
        {
            StartCoroutine(SetBaseline());
        }

        IEnumerator SetBaseline()
        {
            BaselinePupilStorage.Clear();
            baselineInProgress = true;
            baselineValid      = false;

            yield return new WaitForSeconds(BaselineCaptureSeconds);

            if (BaselinePupilStorage.Count > 0)
            {
                baseline      = BaselinePupilStorage.Average();
                baselineValid = true;
            }
            else
            {
                baseline      = 0f;
                baselineValid = false;
            }

            baselineInProgress = false;
            BaselinePupilStorage.Clear();
        }

        float? BaselineCorrected(float value)
        {
            if (!baselineValid || baselineInProgress)
                return null;

            return value - baseline;
        }

        void WriteTrialSummary()
        {
            foreach (var kv in summary)
            {
                if(kv.Key == "Trial")
                {
                    summaryWriter.Write(sxr.GetTrial() -1);
                    summaryWriter.Write(",");
                    continue;
                }
                summaryWriter.Write(kv.Value.Average);
                summaryWriter.Write(",");
            }

            summaryWriter.WriteLine();

            foreach (var kv in summary.Values)
            {
                kv.Sum = 0;
                kv.Count = 0;
            }
        }

        public void StartEventSummaryTimer(float time)
        {
            if (!GenerateEventSummaryFile)
            {
                Debug.LogWarning("Event Summary File generation is disabled.");
                return;
            }

            if (eventCoroutine != null)
                StopCoroutine(eventCoroutine);

            eventCoroutine = StartCoroutine(EventDataCollection(time));
        }

        public void StopEventSummaryTimer()
        {
            isCollectingEventData = false;
        }

        IEnumerator EventDataCollection(float time)
        {
            isCollectingEventData = true;
            yield return new WaitForSeconds(time);
            isCollectingEventData = false;

            foreach (var kv in eventSummary)
            {
                if(kv.Key == "Trial")
                {
                    eventWriter.Write(sxr.GetTrial());
                    eventWriter.Write(",");
                    continue;
                }
                eventWriter.Write(kv.Value.Average);
                eventWriter.Write(",");
            }

            eventWriter.WriteLine();

            foreach (var kv in eventSummary.Values)
            {
                kv.Sum = 0;
                kv.Count = 0;
            }
            eventCoroutine = null;
        }

        void OnApplicationQuit()
        {
            FlushToFile();
            writer?.Close();
            summaryWriter?.Close();
            eventWriter?.Close();
        }

        void OnDestroy()
        {
            FlushToFile();
            writer?.Close();
            summaryWriter?.Close();
            eventWriter?.Close();
        }
    }
}