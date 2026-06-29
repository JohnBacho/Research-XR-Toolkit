using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using VIVE.OpenXR.EyeTracker;
using VIVE.OpenXR.FacialTracking;
using sxr_internal;

namespace VIVE.OpenXR.Samples.FacialTracking
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
        eventBaselineCorrected,
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

       [SerializeField] private string SubjectID = "1";
       [SerializeField] private string DownloadPath = "/sdcard2";
       [SerializeField] private string BackupDownloadPath = "/sdcard";


        public List<DataPoint> dataPoint = new();
        public List<EyeTrackingDataPoints> eyeTrackingDataPoints = new();
        public List<EyeExpressionDataPoints> eyeExpressionDataPoints = new();
        public List<FacialExpressionDataPoints> facialExpressionDataPoints = new();


        private Camera vrCamera;
        private float flushTimer = 0f;

        private readonly StringBuilder writeBuffer = new StringBuilder(1024 * 64);
        private StreamWriter writer;

        private bool recordEyeTracker;
        private bool headerPrinted;

        private string filePath;

        private Vector3 combinedGazeOrigin;
        private Vector3 combinedGazeDirection;

        private Ray gazeRay;
        private RaycastHit hit;

        // -------- PUPIL / BASELINE --------
        private List<float> TempPupilStorage         = new List<float>();
        private List<float> TempBaselinePupilStorage  = new List<float>();
        private List<float> EventBaselinePupilStorage = new List<float>();

        private float leftPupilSize  = 0;
        private float rightPupilSize = 0;

        private float baseline           = 0f;
        private bool  baselineValid      = false;
        private bool  baselineInProgress = false;
        private bool  captureEventBaseline = false;
        private bool hasHit;
        private Vector3 hitPoint;
        private string hitObjectName = "";

        private readonly float[] eyeExp = new float[14];
        private readonly float[] lipExp = new float[37];

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

                EyeTrackingEnum.eventBaselineCorrected =>
                    (captureEventBaseline && CombinedPupil() is float ep
                        ? BaselineCorrected(ep)?.ToString()
                        : "") ?? "",

                EyeTrackingEnum.hitX => hasHit ? hitPoint.x.ToString("F4") : "",
                EyeTrackingEnum.hitY => hasHit ? hitPoint.y.ToString("F4") : "",
                EyeTrackingEnum.hitZ => hasHit ? hitPoint.z.ToString("F4") : "",
                EyeTrackingEnum.objectName => hasHit ? hitObjectName : "",

                _ => ""
            };
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

            Debug.Log("Saving to: " + filePath);

        }

        void Start()
        {
            vrCamera = Camera.main;

            sxr.SetSubjectID("hello");
        }

        void WriteHeader()
        {
            writer.WriteLine(
                "programName,date,localTime,unityTime,phase,trial,trialTimePassed," +
                "gazeOriginX,gazeOriginY,gazeOriginZ," +
                "gazeDirectionX,gazeDirectionY,gazeDirectionZ," +
                "leftPupil,rightPupil,combinedPupil," +
                "baselineCorrected,eventBaselineCorrected," +
                "hitX,hitY,hitZ,objectName," +
                "eyeLeftBlink,eyeLeftWide,eyeRightBlink,eyeRightWide," +
                "eyeLeftSqueeze,eyeRightSqueeze," +
                "eyeLeftDown,eyeRightDown," +
                "eyeLeftOut,eyeRightIn," +
                "eyeLeftIn,eyeRightOut," +
                "eyeLeftUp,eyeRightUp," +
                "jawRight,jawLeft,jawForward,jawOpen," +
                "mouthApeShape," +
                "mouthUpperRight,mouthUpperLeft," +
                "mouthLowerRight,mouthLowerLeft," +
                "mouthUpperOverturn,mouthLowerOverturn," +
                "mouthPout," +
                "mouthRaiserRight,mouthRaiserLeft," +
                "mouthStretcherRight,mouthStretcherLeft," +
                "cheekPuffRight,cheekPuffLeft,cheekSuck," +
                "mouthUpperUpright,mouthUpperUpleft," +
                "mouthLowerDownright,mouthLowerDownleft," +
                "mouthUpperInside,mouthLowerInside," +
                "mouthLowerOverlay," +
                "tongueLongstep1," +
                "tongueLeft,tongueRight,tongueUp,tongueDown," +
                "tongueRoll," +
                "tongueLongstep2," +
                "tongueUprightMorph,tongueUpleftMorph," +
                "tongueDownrightMorph,tongueDownleftMorph," +
                "trialAvgPupil,trialBaselineCorrectedPupil,eventBaselineCorrectedPupil"
            );
            headerPrinted = true;
        }

        void FlushToFile()
        {
            if (writeBuffer.Length == 0) return;
            writer.Write(writeBuffer);
            writer.Flush();
            writeBuffer.Clear();
        }

        void AppendFacialData(StringBuilder sb)
        {
            for (int i = 0; i < EyeExprEnums.Length; i++)
                eyeExp[i] = FacialTrackingData.EyeExpression(EyeExprEnums[i]);

            for (int i = 0; i < LipExprEnums.Length; i++)
                lipExp[i] = FacialTrackingData.LipExpression(LipExprEnums[i]);

            for (int i = 0; i < eyeExp.Length; i++)
            {
                sb.Append(eyeExp[i]);
                sb.Append(',');
            }

            for (int i = 0; i < lipExp.Length; i++)
            {
                sb.Append(lipExp[i]);
                if (i < lipExp.Length - 1) sb.Append(',');
            }
        }

        // -------- RECORDING --------
        public void StartRecording()
        {
            if (!headerPrinted) WriteHeader();
            recordEyeTracker = true;
        }

        public void PauseRecording()
        {
            recordEyeTracker = false;
            FlushToFile();
        }

        public bool RecordingGaze() => recordEyeTracker;

        // -------- UPDATE --------
        void Update()
        {
            UpdateGaze();

            if (recordEyeTracker)
            {
                AppendDataRow(trialAvg: null);
            }

            flushTimer += Time.deltaTime;
            if (flushTimer >= 5f)
            {
                FlushToFile();
                flushTimer = 0f;
            }
        }

        void AppendDataRow(float? trialAvg, float? baselineTrialAvg = null, float? eventBaselineTrialAvg = null)
        {
            UpdatePupil();

            var eh = ExperimentHandler.Instance;
            if (eh != null)
            {
                writeBuffer.Append(eh.subjectID);        writeBuffer.Append(',');
                writeBuffer.Append(DateTime.Today.Month + "_" + DateTime.Today.Day); writeBuffer.Append(',');
                writeBuffer.Append(DateTime.Now.Hour + "_" + DateTime.Now.Minute + "_" + DateTime.Now.Second); writeBuffer.Append(',');
                writeBuffer.Append(Time.time.ToString("F4")); writeBuffer.Append(',');
                writeBuffer.Append(eh.phase);              writeBuffer.Append(',');
                writeBuffer.Append(eh.trial);              writeBuffer.Append(',');
                writeBuffer.Append(eh.GetTimePassed());    writeBuffer.Append(',');
            }
            else
            {
                for (int i = 0; i < 17; i++) writeBuffer.Append(','); // was 15, now 17 columns
            }

            // ── cols 17-19: gaze origin ──
            writeBuffer.Append(combinedGazeOrigin.x); writeBuffer.Append(',');
            writeBuffer.Append(combinedGazeOrigin.y); writeBuffer.Append(',');
            writeBuffer.Append(combinedGazeOrigin.z); writeBuffer.Append(',');

            // ── cols 20-22: gaze direction ──
            writeBuffer.Append(combinedGazeDirection.x); writeBuffer.Append(',');
            writeBuffer.Append(combinedGazeDirection.y); writeBuffer.Append(',');
            writeBuffer.Append(combinedGazeDirection.z); writeBuffer.Append(',');

            // ── cols 23-25: pupils ──
            float? combined          = CombinedPupil();
            float? baselineCorrected = combined.HasValue ? BaselineCorrected(combined.Value) : null;
            float? eventBaseline     = (captureEventBaseline && baselineCorrected.HasValue)
                                           ? baselineCorrected
                                           : (float?)null;

            writeBuffer.Append(leftPupilSize);  writeBuffer.Append(',');
            writeBuffer.Append(rightPupilSize); writeBuffer.Append(',');
            if (combined.HasValue) { writeBuffer.Append(combined.Value); }
            writeBuffer.Append(',');

            // ── cols 26-27: baseline-corrected pupils ──
            if (baselineCorrected.HasValue) { writeBuffer.Append(baselineCorrected.Value); }
            writeBuffer.Append(',');
            if (eventBaseline.HasValue) { writeBuffer.Append(eventBaseline.Value); }
            writeBuffer.Append(',');

            writeBuffer.Append(hasHit ? hitPoint.x.ToString("F4") : ""); writeBuffer.Append(',');
            writeBuffer.Append(hasHit ? hitPoint.y.ToString("F4") : ""); writeBuffer.Append(',');
            writeBuffer.Append(hasHit ? hitPoint.z.ToString("F4") : ""); writeBuffer.Append(',');
            writeBuffer.Append(hasHit ? hitObjectName : "");             writeBuffer.Append(',');

            // ── cols 32-82: facial expressions (14 eye + 37 lip) ──
            AppendFacialData(writeBuffer);

            // ── col 83-85: trial average pupils ──
            writeBuffer.Append(',');
            if (trialAvg.HasValue)          writeBuffer.Append(trialAvg.Value);
            writeBuffer.Append(',');
            if (baselineTrialAvg.HasValue)  writeBuffer.Append(baselineTrialAvg.Value);
            writeBuffer.Append(',');
            if (eventBaselineTrialAvg.HasValue) writeBuffer.Append(eventBaselineTrialAvg.Value);

            writeBuffer.Append('\n');
        }

        // -------- GAZE --------
        void UpdateGaze()
        {
            XR_HTC_eye_tracker.Interop.GetEyeGazeData(out XrSingleEyeGazeDataHTC[] gazes);

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

        // -------- PUPIL --------
        void UpdatePupil()
        {
            XR_HTC_eye_tracker.Interop.GetEyePupilData(out XrSingleEyePupilDataHTC[] pupils);

            var left  = pupils[(int)XrEyePositionHTC.XR_EYE_POSITION_LEFT_HTC];
            var right = pupils[(int)XrEyePositionHTC.XR_EYE_POSITION_RIGHT_HTC];

            leftPupilSize  = left.isDiameterValid  ? left.pupilDiameter  : -1;
            rightPupilSize = right.isDiameterValid ? right.pupilDiameter : -1;

            if (leftPupilSize  > 0) TempPupilStorage.Add(leftPupilSize);
            if (rightPupilSize > 0) TempPupilStorage.Add(rightPupilSize);
        }

        float? CombinedPupil()
        {
            if (leftPupilSize > 0 && rightPupilSize > 0)
                return (leftPupilSize + rightPupilSize) / 2f;
            if (leftPupilSize  > 0) return leftPupilSize;
            if (rightPupilSize > 0) return rightPupilSize;
            return null;
        }

        float? BaselineCorrected(float value)
        {
            if (!baselineValid || baselineInProgress) return null;
            float corrected = value - baseline;
            TempBaselinePupilStorage.Add(corrected);
            if (captureEventBaseline) EventBaselinePupilStorage.Add(corrected);
            return corrected;
        }

        // -------- TRIAL AVG --------
        public void GrabPupilTrialAverage()
        {
            if (TempPupilStorage.Count == 0) return;

            float  trialAvg          = TempPupilStorage.Average();
            float? baselineAvg       = (!baselineValid || TempBaselinePupilStorage.Count == 0)
                                        ? (float?)null
                                        : TempBaselinePupilStorage.Average();
            float? eventBaselineAvg  = (!baselineValid || EventBaselinePupilStorage.Count == 0)
                                        ? (float?)null
                                        : EventBaselinePupilStorage.Average();

            AppendDataRow(trialAvg: trialAvg, baselineTrialAvg: baselineAvg, eventBaselineTrialAvg: eventBaselineAvg);

            TempPupilStorage.Clear();
            TempBaselinePupilStorage.Clear();
            EventBaselinePupilStorage.Clear();
            captureEventBaseline = false;
        }

        // -------- BASELINE --------
        public void StartBaseline()
        {
            StartCoroutine(SetBaseline());
        }

        IEnumerator SetBaseline()
        {
            TempPupilStorage.Clear();
            baselineInProgress = true;
            baselineValid      = false;

            yield return new WaitForSeconds(1f);

            if (TempPupilStorage.Count > 0)
            {
                baseline      = TempPupilStorage.Average();
                baselineValid = true;
            }
            else
            {
                baseline      = 0f;
                baselineValid = false;
            }

            baselineInProgress = false;
            TempPupilStorage.Clear();
            TempBaselinePupilStorage.Clear();
        }

        public void SetCaptureEventBaseline()
        {
            captureEventBaseline = true;
        }

        void OnApplicationQuit()
        {
            FlushToFile();
            writer?.Close();
        }

        void OnDestroy()
        {
            FlushToFile();
            writer?.Close();
        }
    }
}