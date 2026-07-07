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

namespace RXT
{
    [System.Serializable]
    public class DataChannel
    {
        [HideInInspector] public string DataField;
        public string Header;
        public bool Toggle = true;
    }

    [System.Serializable]
    public class DataChannelGroup
    {
        public bool Enabled = true;
        public List<DataChannel> Channels = new();
    }

    [System.Serializable]
    class SummaryValue
    {
        public double Sum;
        public int Count;
        public string Text;

        public void Add(double value)
        {
            Sum += value;
            Count++;
        }

        public void SetText(string value)
        {
            Text = value;
        }

        public bool IsNumeric => Count > 0;

        public string Output => IsNumeric ? Average.ToString() : Text;

        public double Average => Count == 0 ? 0 : Sum / Count;
    }

    public class DataCollection : MonoBehaviour
    {
        [ContextMenu("Populate All Data Points")]
        private void PopulateAll()
        {
            dataPoint = BasicFieldDefs
                .Select(d => new DataChannel { Toggle = true, DataField = d.DataField, Header = d.DataField })
                .ToList();
            cameraTrackingDataPoints = CameraTrackingDefs
                .Select(d => new DataChannel { Toggle = true, DataField = d.DataField, Header = d.DataField })
                .ToList();
            eyeTrackingDataPoints = EyeTrackingDefs
                .Select(d => new DataChannel { Toggle = true, DataField = d.DataField, Header = d.DataField })
                .ToList();
            eyeExpressionDataPoints = EyeExpressionDefs
                .Select(d => new DataChannel { Toggle = true, DataField = d.DataField, Header = d.DataField })
                .ToList();
            facialExpressionDataPoints = FacialExpressionDefs
                .Select(d => new DataChannel { Toggle = true, DataField = d.DataField, Header = d.DataField })
                .ToList();
            BasicData.Channels = dataPoint;
            CameraTracking.Channels = cameraTrackingDataPoints;
            EyeTracking.Channels = eyeTrackingDataPoints;
            EyeExpressions.Channels = eyeExpressionDataPoints;
            FacialExpressions.Channels = facialExpressionDataPoints;
        }

        public static DataCollection Instance;

        [SerializeField] private string DownloadPath = "Data";
        [SerializeField] private string BackupDownloadPath = "sdcard";
        [SerializeField] private bool RunOnStartup = true;
        [SerializeField] private bool GenerateTrialSummaryFile;
        [SerializeField] private bool GenerateEventSummaryFile;
        [SerializeField] private bool RecaptureBaselineOnTrialChange = false;

        private float BaselineCaptureSeconds;


        private static readonly (string DataField, Func<DataCollection, string> GetValue)[] BasicFieldDefs =
        {
            ("SubjectID",        d => sxr.GetSubjectID()),
            ("Date",             d => DateTime.Today.ToString("M_d")),
            ("LocalTime",        d => DateTime.Now.ToString("H_m_s")),
            ("UnityTime",        d => Time.time.ToString("F4")),
            ("Phase",            d => sxr.GetPhase().ToString()),
            ("Trial",            d => sxr.GetTrial().ToString()),
            ("Step",             d => sxr.GetStepInTrial().ToString()),
            ("TrialTimePassed",  d => sxr.TimePassed().ToString()),
            ("State",            d => sxr.GetState()),
        };

        private static readonly (string DataField, Func<DataCollection, string> GetValue)[] CameraTrackingDefs =
        {
            ("xPos",        d => d.UpdateCamera("xPos")),
            ("yPos",             d => d.UpdateCamera("yPos")),
            ("zPos",        d => d.UpdateCamera("zPos")),
            ("xRot",        d => d.UpdateCamera("xRot")),
            ("yRot",            d => d.UpdateCamera("yRot")),
            ("zRot",            d => d.UpdateCamera("zRot")),
        };

        private static readonly (string DataField, Func<DataCollection, string> GetValue)[] EyeTrackingDefs =
        {
            ("gazeOriginX",        d => d.combinedGazeOrigin.x.ToString("F4")),
            ("gazeOriginY",        d => d.combinedGazeOrigin.y.ToString("F4")),
            ("gazeOriginZ",        d => d.combinedGazeOrigin.z.ToString("F4")),
            ("gazeDirectionX",        d => d.combinedGazeDirection.x.ToString("F4")),
            ("gazeDirectionY",            d => d.combinedGazeDirection.y.ToString("F4")),
            ("gazeDirectionZ",            d => d.combinedGazeDirection.z.ToString("F4")),
            ("leftPupil",             d => d.leftPupilSize.ToString()),
            ("rightPupil",  d => d.rightPupilSize.ToString()),
            ("combinedPupil", d => d.CombinedPupil()?.ToString() ?? ""),
            ("baselineCorrected", d =>
            {
                float? cp = d.CombinedPupil();
                return cp.HasValue ? (d.BaselineCorrected(cp.Value)?.ToString() ?? "") : "";
            }),
            ("hitX", d => d.hasHit ? d.hitPoint.x.ToString("F4") : ""),
            ("hitY", d => d.hasHit ? d.hitPoint.y.ToString("F4") : ""),
            ("hitZ", d => d.hasHit ? d.hitPoint.z.ToString("F4") : ""),
            ("objectName", d => d.hasHit ? d.hitObjectName : ""),
        };

        private static readonly (string DataField, Func<DataCollection, string> GetValue)[] EyeExpressionDefs =
        {
            ("EYE_EXPRESSION_LEFT_BLINK_HTC",        d => d.GetEyeExpressionValue(XrEyeExpressionHTC.XR_EYE_EXPRESSION_LEFT_BLINK_HTC)),
            ("EYE_EXPRESSION_LEFT_WIDE_HTC",        d => d.GetEyeExpressionValue(XrEyeExpressionHTC.XR_EYE_EXPRESSION_LEFT_WIDE_HTC)),
            ("EYE_EXPRESSION_RIGHT_BLINK_HTC",        d => d.GetEyeExpressionValue(XrEyeExpressionHTC.XR_EYE_EXPRESSION_RIGHT_BLINK_HTC)),
            ("EYE_EXPRESSION_RIGHT_WIDE_HTC",        d => d.GetEyeExpressionValue(XrEyeExpressionHTC.XR_EYE_EXPRESSION_RIGHT_WIDE_HTC)),
            ("EYE_EXPRESSION_LEFT_SQUEEZE_HTC",            d => d.GetEyeExpressionValue(XrEyeExpressionHTC.XR_EYE_EXPRESSION_LEFT_SQUEEZE_HTC)),
            ("EYE_EXPRESSION_RIGHT_SQUEEZE_HTC",            d => d.GetEyeExpressionValue(XrEyeExpressionHTC.XR_EYE_EXPRESSION_RIGHT_SQUEEZE_HTC)),
            ("EYE_EXPRESSION_LEFT_DOWN_HTC",             d => d.GetEyeExpressionValue(XrEyeExpressionHTC.XR_EYE_EXPRESSION_LEFT_DOWN_HTC)),
            ("EYE_EXPRESSION_RIGHT_DOWN_HTC",  d => d.GetEyeExpressionValue(XrEyeExpressionHTC.XR_EYE_EXPRESSION_RIGHT_DOWN_HTC)),
            ("EYE_EXPRESSION_LEFT_OUT_HTC", d => d.GetEyeExpressionValue(XrEyeExpressionHTC.XR_EYE_EXPRESSION_LEFT_OUT_HTC)),
            ("EYE_EXPRESSION_RIGHT_IN_HTC", d => d.GetEyeExpressionValue(XrEyeExpressionHTC.XR_EYE_EXPRESSION_RIGHT_IN_HTC)),
            ("EYE_EXPRESSION_LEFT_IN_HTC", d => d.GetEyeExpressionValue(XrEyeExpressionHTC.XR_EYE_EXPRESSION_LEFT_IN_HTC)),
            ("EYE_EXPRESSION_RIGHT_OUT_HTC", d => d.GetEyeExpressionValue(XrEyeExpressionHTC.XR_EYE_EXPRESSION_RIGHT_OUT_HTC)),
            ("EYE_EXPRESSION_LEFT_UP_HTC", d => d.GetEyeExpressionValue(XrEyeExpressionHTC.XR_EYE_EXPRESSION_LEFT_UP_HTC)),
            ("EYE_EXPRESSION_RIGHT_UP_HTC", d => d.GetEyeExpressionValue(XrEyeExpressionHTC.XR_EYE_EXPRESSION_RIGHT_UP_HTC)),
        };

        private static readonly (string DataField, Func<DataCollection, string> GetValue)[] FacialExpressionDefs =
        {
            ("XR_LIP_EXPRESSION_JAW_RIGHT_HTC",           d => d.GetFacialExpressionValue(XrLipExpressionHTC.XR_LIP_EXPRESSION_JAW_RIGHT_HTC)),
            ("XR_LIP_EXPRESSION_JAW_LEFT_HTC",            d => d.GetFacialExpressionValue(XrLipExpressionHTC.XR_LIP_EXPRESSION_JAW_LEFT_HTC)),
            ("XR_LIP_EXPRESSION_JAW_FORWARD_HTC",         d => d.GetFacialExpressionValue(XrLipExpressionHTC.XR_LIP_EXPRESSION_JAW_FORWARD_HTC)),
            ("XR_LIP_EXPRESSION_JAW_OPEN_HTC",            d => d.GetFacialExpressionValue(XrLipExpressionHTC.XR_LIP_EXPRESSION_JAW_OPEN_HTC)),
            ("XR_LIP_EXPRESSION_MOUTH_APE_SHAPE_HTC",     d => d.GetFacialExpressionValue(XrLipExpressionHTC.XR_LIP_EXPRESSION_MOUTH_APE_SHAPE_HTC)),
            ("XR_LIP_EXPRESSION_MOUTH_UPPER_RIGHT_HTC",   d => d.GetFacialExpressionValue(XrLipExpressionHTC.XR_LIP_EXPRESSION_MOUTH_UPPER_RIGHT_HTC)),
            ("XR_LIP_EXPRESSION_MOUTH_UPPER_LEFT_HTC",    d => d.GetFacialExpressionValue(XrLipExpressionHTC.XR_LIP_EXPRESSION_MOUTH_UPPER_LEFT_HTC)),
            ("XR_LIP_EXPRESSION_MOUTH_LOWER_RIGHT_HTC",   d => d.GetFacialExpressionValue(XrLipExpressionHTC.XR_LIP_EXPRESSION_MOUTH_LOWER_RIGHT_HTC)),
            ("XR_LIP_EXPRESSION_MOUTH_LOWER_LEFT_HTC",    d => d.GetFacialExpressionValue(XrLipExpressionHTC.XR_LIP_EXPRESSION_MOUTH_LOWER_LEFT_HTC)),
            ("XR_LIP_EXPRESSION_MOUTH_UPPER_OVERTURN_HTC",d => d.GetFacialExpressionValue(XrLipExpressionHTC.XR_LIP_EXPRESSION_MOUTH_UPPER_OVERTURN_HTC)),
            ("XR_LIP_EXPRESSION_MOUTH_LOWER_OVERTURN_HTC",d => d.GetFacialExpressionValue(XrLipExpressionHTC.XR_LIP_EXPRESSION_MOUTH_LOWER_OVERTURN_HTC)),
            ("XR_LIP_EXPRESSION_MOUTH_POUT_HTC",          d => d.GetFacialExpressionValue(XrLipExpressionHTC.XR_LIP_EXPRESSION_MOUTH_POUT_HTC)),
            ("XR_LIP_EXPRESSION_MOUTH_RAISER_RIGHT_HTC",  d => d.GetFacialExpressionValue(XrLipExpressionHTC.XR_LIP_EXPRESSION_MOUTH_RAISER_RIGHT_HTC)),
            ("XR_LIP_EXPRESSION_MOUTH_RAISER_LEFT_HTC",   d => d.GetFacialExpressionValue(XrLipExpressionHTC.XR_LIP_EXPRESSION_MOUTH_RAISER_LEFT_HTC)),
            ("XR_LIP_EXPRESSION_MOUTH_STRETCHER_RIGHT_HTC",d => d.GetFacialExpressionValue(XrLipExpressionHTC.XR_LIP_EXPRESSION_MOUTH_STRETCHER_RIGHT_HTC)),
            ("XR_LIP_EXPRESSION_MOUTH_STRETCHER_LEFT_HTC",d => d.GetFacialExpressionValue(XrLipExpressionHTC.XR_LIP_EXPRESSION_MOUTH_STRETCHER_LEFT_HTC)),
            ("XR_LIP_EXPRESSION_CHEEK_PUFF_RIGHT_HTC",    d => d.GetFacialExpressionValue(XrLipExpressionHTC.XR_LIP_EXPRESSION_CHEEK_PUFF_RIGHT_HTC)),
            ("XR_LIP_EXPRESSION_CHEEK_PUFF_LEFT_HTC",     d => d.GetFacialExpressionValue(XrLipExpressionHTC.XR_LIP_EXPRESSION_CHEEK_PUFF_LEFT_HTC)),
            ("XR_LIP_EXPRESSION_CHEEK_SUCK_HTC",          d => d.GetFacialExpressionValue(XrLipExpressionHTC.XR_LIP_EXPRESSION_CHEEK_SUCK_HTC)),
            ("XR_LIP_EXPRESSION_MOUTH_UPPER_UPRIGHT_HTC", d => d.GetFacialExpressionValue(XrLipExpressionHTC.XR_LIP_EXPRESSION_MOUTH_UPPER_UPRIGHT_HTC)),
            ("XR_LIP_EXPRESSION_MOUTH_UPPER_UPLEFT_HTC",  d => d.GetFacialExpressionValue(XrLipExpressionHTC.XR_LIP_EXPRESSION_MOUTH_UPPER_UPLEFT_HTC)),
            ("XR_LIP_EXPRESSION_MOUTH_LOWER_DOWNRIGHT_HTC",d => d.GetFacialExpressionValue(XrLipExpressionHTC.XR_LIP_EXPRESSION_MOUTH_LOWER_DOWNRIGHT_HTC)),
            ("XR_LIP_EXPRESSION_MOUTH_LOWER_DOWNLEFT_HTC",d => d.GetFacialExpressionValue(XrLipExpressionHTC.XR_LIP_EXPRESSION_MOUTH_LOWER_DOWNLEFT_HTC)),
            ("XR_LIP_EXPRESSION_MOUTH_UPPER_INSIDE_HTC",  d => d.GetFacialExpressionValue(XrLipExpressionHTC.XR_LIP_EXPRESSION_MOUTH_UPPER_INSIDE_HTC)),
            ("XR_LIP_EXPRESSION_MOUTH_LOWER_INSIDE_HTC",  d => d.GetFacialExpressionValue(XrLipExpressionHTC.XR_LIP_EXPRESSION_MOUTH_LOWER_INSIDE_HTC)),
            ("XR_LIP_EXPRESSION_MOUTH_LOWER_OVERLAY_HTC", d => d.GetFacialExpressionValue(XrLipExpressionHTC.XR_LIP_EXPRESSION_MOUTH_LOWER_OVERLAY_HTC)),
            ("XR_LIP_EXPRESSION_TONGUE_LONGSTEP1_HTC",    d => d.GetFacialExpressionValue(XrLipExpressionHTC.XR_LIP_EXPRESSION_TONGUE_LONGSTEP1_HTC)),
            ("XR_LIP_EXPRESSION_TONGUE_LEFT_HTC",         d => d.GetFacialExpressionValue(XrLipExpressionHTC.XR_LIP_EXPRESSION_TONGUE_LEFT_HTC)),
            ("XR_LIP_EXPRESSION_TONGUE_RIGHT_HTC",        d => d.GetFacialExpressionValue(XrLipExpressionHTC.XR_LIP_EXPRESSION_TONGUE_RIGHT_HTC)),
            ("XR_LIP_EXPRESSION_TONGUE_UP_HTC",           d => d.GetFacialExpressionValue(XrLipExpressionHTC.XR_LIP_EXPRESSION_TONGUE_UP_HTC)),
            ("XR_LIP_EXPRESSION_TONGUE_DOWN_HTC",         d => d.GetFacialExpressionValue(XrLipExpressionHTC.XR_LIP_EXPRESSION_TONGUE_DOWN_HTC)),
            ("XR_LIP_EXPRESSION_TONGUE_ROLL_HTC",         d => d.GetFacialExpressionValue(XrLipExpressionHTC.XR_LIP_EXPRESSION_TONGUE_ROLL_HTC)),
            ("XR_LIP_EXPRESSION_TONGUE_LONGSTEP2_HTC",    d => d.GetFacialExpressionValue(XrLipExpressionHTC.XR_LIP_EXPRESSION_TONGUE_LONGSTEP2_HTC)),
            ("XR_LIP_EXPRESSION_TONGUE_UPRIGHT_MORPH_HTC",d => d.GetFacialExpressionValue(XrLipExpressionHTC.XR_LIP_EXPRESSION_TONGUE_UPRIGHT_MORPH_HTC)),
            ("XR_LIP_EXPRESSION_TONGUE_UPLEFT_MORPH_HTC", d => d.GetFacialExpressionValue(XrLipExpressionHTC.XR_LIP_EXPRESSION_TONGUE_UPLEFT_MORPH_HTC)),
            ("XR_LIP_EXPRESSION_TONGUE_DOWNRIGHT_MORPH_HTC",d => d.GetFacialExpressionValue(XrLipExpressionHTC.XR_LIP_EXPRESSION_TONGUE_DOWNRIGHT_MORPH_HTC)),
            ("XR_LIP_EXPRESSION_TONGUE_DOWNLEFT_MORPH_HTC", d => d.GetFacialExpressionValue(XrLipExpressionHTC.XR_LIP_EXPRESSION_TONGUE_DOWNLEFT_MORPH_HTC)),
        };

        private List<DataChannel> dataPoint = new();
        private List<DataChannel> cameraTrackingDataPoints = new();
        private List<DataChannel> eyeTrackingDataPoints = new();
        private List<DataChannel> eyeExpressionDataPoints = new();
        private List<DataChannel> facialExpressionDataPoints = new();
        public DataChannelGroup BasicData;
        public DataChannelGroup CameraTracking;
        public DataChannelGroup EyeTracking;
        public DataChannelGroup EyeExpressions;
        public DataChannelGroup FacialExpressions;
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

        void Start()
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

            string storageRoot = Path.Combine(Application.dataPath, DownloadPath);
            try
            {
                if (!Directory.Exists(storageRoot))
                    Directory.CreateDirectory(storageRoot);
            }
            catch
            {
                storageRoot = Path.Combine(Application.dataPath, BackupDownloadPath);
            }

            string folderName = "";

            string subfolderBase = folderName.ToLowerInvariant();

            string subfolderName =$"{subfolderBase}{rxt.GetUniqueID().ToString()}";
            string subfolderPath = Path.Combine(storageRoot, subfolderName);

            Directory.CreateDirectory(subfolderPath);

            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            filePath = Path.Combine(subfolderPath, $"Data_{timestamp}.csv");

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

                vrCamera = Camera.main;

            if(RunOnStartup)
            {
                StartRecording();
            }
            sxr.SetSubjectID(rxt.GetUniqueID().ToString());
        }

        string BuildHeader(DataChannelGroup data)
        {
            string headerConstructor = "";
            foreach(var dp in data.Channels)
            {
                if (!dp.Toggle || !data.Enabled)
                {
                    continue;
                }
                headerConstructor += dp.Header + ",";
                if(GenerateTrialSummaryFile) summary[dp.DataField] = new SummaryValue();
                if(GenerateEventSummaryFile) eventSummary[dp.DataField] = new SummaryValue();

            }

            return headerConstructor;
        }

        void WriteHeader()
        {
            string header = "";

            header += BuildHeader(BasicData);
            header += BuildHeader(CameraTracking);
            header += BuildHeader(EyeTracking);
            header += BuildHeader(EyeExpressions);
            header += BuildHeader(FacialExpressions);

            writer.WriteLine(header.TrimEnd(','));
            if(GenerateTrialSummaryFile) summaryWriter.WriteLine(header.TrimEnd(','));
            if(GenerateEventSummaryFile) eventWriter.WriteLine(header.TrimEnd(','));

            headerPrinted = true;
        }

        void FlushToFile()
        {
            if (writeBuffer.Length == 0) return;
            writer.Write(writeBuffer);
            writer.Flush();
            writeBuffer.Clear();
        }


    void AppendChannelGroup(
    StringBuilder sb,
    DataChannelGroup group,
    (string DataField, Func<DataCollection, string> GetValue)[] defs)
    {
        if (!group.Enabled)
            return;

        foreach (var dp in group.Channels)
        {
            if (!dp.Toggle) continue;

            var def = Array.Find(defs, d => d.DataField == dp.DataField);
            if (def.GetValue == null)
            {
                Debug.LogWarning($"No matching field def for header '{dp.DataField}' — skipping.");
                continue;
            }

            string value = def.GetValue(this);

            if (double.TryParse(value, out double number))
            {
                if (GenerateTrialSummaryFile)
                    summary[dp.DataField].Add(number);

                if (GenerateEventSummaryFile && isCollectingEventData)
                    eventSummary[dp.DataField].Add(number);
            }
            else
            {
                if (GenerateTrialSummaryFile)
                    summary[dp.DataField].SetText(value);

                if (GenerateEventSummaryFile && isCollectingEventData)
                    eventSummary[dp.DataField].SetText(value);
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
            CheckForChangeInTrial();

            flushTimer += Time.deltaTime;
            if (flushTimer >= 5f)
            {
                FlushToFile();
                flushTimer = 0f;
            }
        }

        void AppendDataRow()
        {

            AppendChannelGroup(writeBuffer, BasicData, BasicFieldDefs);
            AppendChannelGroup(writeBuffer, CameraTracking, CameraTrackingDefs);
            AppendChannelGroup(writeBuffer, EyeTracking, EyeTrackingDefs);
            AppendChannelGroup(writeBuffer, EyeExpressions, EyeExpressionDefs);
            AppendChannelGroup(writeBuffer, FacialExpressions, FacialExpressionDefs);
            writeBuffer.Append("\r\n");
        }

        string UpdateCamera(string dataField)
        {
            var trans = vrCamera.transform;
            var pos = trans.position;
            var rot = trans.rotation;
            switch (dataField)
            {
                case "xPos":
                    return pos.x.ToString();
                case "yPos":
                    return pos.y.ToString();
                case "zPos":
                    return pos.z.ToString();
                case "xRot":
                    return rot.eulerAngles.x.ToString();
                case "yRot":
                    return rot.eulerAngles.y.ToString();
                case "zRot":
                    return rot.eulerAngles.z.ToString();
            }

            return "";
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

                if (GenerateTrialSummaryFile)
                {
                    StartBaseline();
                }
            }
        }

        public void StartBaseline(float BaselineCaptureDuration = 1f)
        {
            if (BaselineCaptureDuration > 0f)
                BaselineCaptureSeconds = BaselineCaptureDuration;

            if (baselineInProgress)
            {
                Debug.LogWarning("Baseline capture already in progress.");
                return;
            }

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
                summaryWriter.Write(kv.Value.Output);
                summaryWriter.Write(",");
            }

            summaryWriter.WriteLine();

            foreach (var kv in summary.Values)
            {
                kv.Sum = 0;
                kv.Count = 0;
                kv.Text = null;
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
                eventWriter.Write(kv.Value.Output);
                eventWriter.Write(",");
            }

            eventWriter.WriteLine();

            foreach (var kv in eventSummary.Values)
            {
                kv.Sum = 0;
                kv.Count = 0;
                kv.Text = null;
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