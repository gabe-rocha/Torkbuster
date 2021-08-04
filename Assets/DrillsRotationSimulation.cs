using System.Transactions;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DrillsRotationSimulation : MonoBehaviour
{
    public float currentRPM = 50;
    public float rpmIncreaseStep = 10f;
    public float rpmIncreaseWithShock = 50f;
    public float minRPM = 0f;
    public float maxRPM = 200f;
    public float rpmReductionDuration = 3f;
    public float vibrationFrequency = 0.1f;
    public float vibrationAmplitude = 0.0025f;
    public float vibrationShockFactor = 2f;
    public float stutteringChance = 0.3f;
    public float stutterDuration = 1f;
    public float stutterMinInterval = 2f;
    public float stutteringFactor = 2f;
    public float torkbusterVibeAndFreqReductionFactor = 0.5f;
    public Transform torkBuster;
    public ParticleSystem particlesBlue, particlesRed;
    public TextMeshProUGUI textRPM, textStutter;
    public Color stutterColor;
    public Transform drillToBeReplaced;
    public Slider rpmSlider;
    public Transform rpmGaugeArrow;


    private bool isTorkbusterInstalled = false;
    private int indexDrillReplacement = 2;

    private bool isStopped = false;
    private float zRotationEulersPerSecond = 0;
    private Coroutine shockCoRo, coroRemovingOldDrillOut, coroInstallingOldDrill, coroInstallingTorkBuster, coroRemovingTorkBuster;
    private bool isShocking = false;
    private float oldRPM;
    // private Vector3 drillInitialPos;
    private bool isDrillMovingUp = true;
    private Vector3 targetPosUp, targetPosDown;
    private float defaultVibrationFrequency, defaultVibrationAmplitude;
    private bool isStuttering = false;
    private float stutterStartTime, lastStutterDraw;
    private float maxFreq, maxAmp;
    private float rpmToGaugeArrowfactor;


    private void Start()
    {
        zRotationEulersPerSecond = 360f * currentRPM / 60f;
        targetPosUp = Vector3.up * vibrationAmplitude;
        targetPosDown = -1 * Vector3.up * vibrationAmplitude;
        oldRPM = currentRPM;
        defaultVibrationFrequency = vibrationFrequency;
        defaultVibrationAmplitude = vibrationAmplitude;

        maxFreq = defaultVibrationFrequency * 3f;
        maxAmp = defaultVibrationAmplitude * 3f;

        rpmSlider.value = currentRPM;
        rpmSlider.onValueChanged.AddListener (delegate {RPMSliderValueChanged ();});
        
        rpmToGaugeArrowfactor = -144.5f / maxRPM; //200 rpm is 144.5 degrees

        UpdateUI();
    }

    public void RPMSliderValueChanged()
	{
        if(isShocking){
            return;
        }
		currentRPM = rpmSlider.value;
        oldRPM = currentRPM;
        zRotationEulersPerSecond = 360f * currentRPM / 60f;
        UpdateUI();
	}

    private void Update()
    {
        RotateDrillsContinuously();
        VibrateDrillsContinuously();
        StutterDrills();
        AnimateRPMGaugeArrow();
    }



    private void RotateDrillsContinuously()
    {
        //transform.Rotate(Vector3.forward * zRotationEulersPerSecond * Time.deltaTime, Space.Self);
        torkBuster.Rotate(-1f * Vector3.right * zRotationEulersPerSecond * Time.deltaTime, Space.Self);
    }

    private void VibrateDrillsContinuously(){
        if(!isTorkbusterInstalled){
            //Stutter sometimes when below 150 RPM
            if(currentRPM < 150 && !isStuttering && Time.time > lastStutterDraw + stutterMinInterval && !isShocking){
                if(Random.value <= stutteringChance){
                    stutterStartTime = Time.time;
                    isStuttering = true;
                    UpdateUI();
                    
                    //Apply stuttering multiplier
                    vibrationFrequency *= stutteringFactor;
                    vibrationAmplitude *= stutteringFactor;
                }
                lastStutterDraw = Time.time;
            }

            //finish stuttering after duration
            if(isStuttering && Time.time > stutterStartTime + stutterDuration){
                isStuttering = false;
                UpdateUI();
                vibrationFrequency = defaultVibrationFrequency;
                vibrationAmplitude = defaultVibrationAmplitude;
            }
        }
        else{
            // var defaultPos = transform.position;
            // defaultPos.y = 0;
            // transform.position = defaultPos;

            // vibrationAmplitude = defaultVibrationAmplitude / 4f;
            // vibrationFrequency = defaultVibrationFrequency / 4f;
        }

        //Calculate target positions
        targetPosUp = Vector3.up * vibrationAmplitude * currentRPM;
        targetPosDown = -1 * Vector3.up * vibrationAmplitude * currentRPM;

        if(isDrillMovingUp)
        {
            if(transform.position.y < targetPosUp.y){
                targetPosUp.x = transform.position.x;
                targetPosUp.z = transform.position.z;
                transform.position = Vector3.MoveTowards(transform.position, targetPosUp, vibrationFrequency * currentRPM * Time.deltaTime);
                
                Debug.Log("Moving UP");
            }
            else{
                targetPosUp.x = transform.position.x;
                targetPosUp.z = transform.position.z;
                transform.position = targetPosUp;
                isDrillMovingUp = false;
            }
        }
        else{ //moving down
            if(transform.position.y > targetPosDown.y){
                targetPosDown.x = transform.position.x;
                targetPosDown.z = transform.position.z;
                transform.position = Vector3.MoveTowards(transform.position, targetPosDown, vibrationFrequency * currentRPM * Time.deltaTime);

                Debug.Log("Moving DOWN");
            }
            else{
                targetPosDown.x = transform.position.x;
                targetPosDown.z = transform.position.z;
                transform.position = targetPosDown;
                isDrillMovingUp = true;
            }
        }
        
    }
    

    private void StutterDrills(){
        if(isTorkbusterInstalled){
            return;
        }
    }

    public void OnButtonInstallTorkbusterPressed(){
        
        isTorkbusterInstalled = !isTorkbusterInstalled;
        
        if(isTorkbusterInstalled){
            vibrationAmplitude = defaultVibrationAmplitude * torkbusterVibeAndFreqReductionFactor;
            vibrationFrequency = defaultVibrationFrequency * torkbusterVibeAndFreqReductionFactor;

            drillToBeReplaced.gameObject.GetComponentInChildren<MeshRenderer>().enabled = false;
            torkBuster.gameObject.SetActive(true);
            isStuttering = false;
            UpdateUI();
        }
        else{
            vibrationAmplitude = defaultVibrationAmplitude;
            vibrationFrequency = defaultVibrationFrequency;
            drillToBeReplaced.gameObject.GetComponentInChildren<MeshRenderer>().enabled = true;
            torkBuster.gameObject.SetActive(false);
            UpdateUI();
        }
        
    }

    public void OnButtonIncreaseRPMPressed(){
        IncreaseRPM();
    }

    public void OnButtonDecreaseRPMPressed(){
        DecreaseRPM();
    }

    private void IncreaseRPM(){
        if(currentRPM < maxRPM){
            currentRPM += rpmIncreaseStep;
            Mathf.Clamp(currentRPM, minRPM, maxRPM);
            zRotationEulersPerSecond = 360f * currentRPM / 60f;
            oldRPM = currentRPM;
            UpdateUI();
        }
    }

    private void DecreaseRPM(){
        if(currentRPM > minRPM){
            currentRPM -= rpmIncreaseStep;
            Mathf.Clamp(currentRPM, minRPM, maxRPM);
            zRotationEulersPerSecond = 360f * currentRPM / 60f;
            oldRPM = currentRPM;
            UpdateUI();
        }
    }

    private void IncreaseRPMAndVibeDueToShock(){
        if(currentRPM < maxRPM){
            currentRPM += rpmIncreaseWithShock;
            currentRPM = Mathf.Clamp(currentRPM, minRPM, maxRPM);
            zRotationEulersPerSecond = 360f * currentRPM / 60f;

            if(isTorkbusterInstalled){
                vibrationFrequency *= vibrationShockFactor;
                vibrationFrequency = Mathf.Clamp(vibrationFrequency, defaultVibrationFrequency, maxFreq * torkbusterVibeAndFreqReductionFactor);
                
                vibrationAmplitude *= vibrationShockFactor;
                vibrationAmplitude = Mathf.Clamp(vibrationAmplitude, defaultVibrationAmplitude, maxAmp * torkbusterVibeAndFreqReductionFactor);
            }
            else{
                vibrationFrequency *= vibrationShockFactor;
                vibrationFrequency = Mathf.Clamp(vibrationFrequency, defaultVibrationFrequency, maxFreq);
                
                vibrationAmplitude *= vibrationShockFactor;
                vibrationAmplitude = Mathf.Clamp(vibrationAmplitude, defaultVibrationAmplitude, maxAmp);
            }

            isShocking = true;

            UpdateUI();

            if(shockCoRo != null){
                StopCoroutine(shockCoRo);
            }
            shockCoRo = StartCoroutine(RPMAndVibeReductionAfterSeconds());
        }
    }

    IEnumerator RPMAndVibeReductionAfterSeconds(){
        float startTime = Time.time;
        float deltaRPM = currentRPM - oldRPM;
        
        float deltaVibeFreq, deltaVibeAmp;
        if(isTorkbusterInstalled){
            deltaVibeFreq = vibrationFrequency - (defaultVibrationFrequency * torkbusterVibeAndFreqReductionFactor);
            deltaVibeAmp = vibrationAmplitude - (defaultVibrationAmplitude * torkbusterVibeAndFreqReductionFactor);
        }
        else{            
            deltaVibeFreq = vibrationFrequency - defaultVibrationFrequency;
            deltaVibeAmp = vibrationAmplitude - defaultVibrationAmplitude;
        }

        while(Time.time < startTime + rpmReductionDuration){
            currentRPM -= deltaRPM / rpmReductionDuration * Time.deltaTime;
            zRotationEulersPerSecond = 360f * currentRPM / 60f;
            vibrationFrequency -= deltaVibeFreq / rpmReductionDuration * Time.deltaTime;
            vibrationAmplitude -= deltaVibeAmp / rpmReductionDuration * Time.deltaTime;
            
            UpdateUI();
            yield return null;
        }
        currentRPM = oldRPM;
        zRotationEulersPerSecond = 360f * currentRPM / 60f;

        if(isTorkbusterInstalled){
            vibrationFrequency = defaultVibrationFrequency * torkbusterVibeAndFreqReductionFactor;
            vibrationAmplitude = defaultVibrationAmplitude * torkbusterVibeAndFreqReductionFactor;
        }
        else{
            vibrationFrequency = defaultVibrationFrequency;
            vibrationAmplitude = defaultVibrationAmplitude;
        }
        isShocking = false;
        UpdateUI();      
    }

    public void OnButtonShockPressed(){
        
        IncreaseRPMAndVibeDueToShock();

        if(isTorkbusterInstalled){
            var particle = Instantiate(particlesBlue);
            particle.Play();
            Destroy(particle, 15f);
        }
        else{
            var particle = Instantiate(particlesRed);
            particle.Play();
            Destroy(particle, 15f);
        }
    }

    IEnumerator RemoveTorkBuster(){
        yield return null;
    }
    IEnumerator InstallTorkBuster(){
        yield return null;
    }

    private void AnimateRPMGaugeArrow(){
        var defaultRotationZ = currentRPM * rpmToGaugeArrowfactor;
        
        rpmGaugeArrow.localRotation = Quaternion.Euler(0f,0f, currentRPM * rpmToGaugeArrowfactor + Random.Range(-currentRPM/100f, currentRPM/100f));
    }

    private void UpdateUI(){
        textRPM.text = currentRPM.ToString($"000") + " RPM";
        textStutter.color = isStuttering ? stutterColor : Color.gray;
        rpmSlider.value = currentRPM;

        // rpmGaugeArrow.localRotation = Quaternion.Euler(0f,0f, currentRPM * rpmToGaugeArrowfactor);
    }

    // private void OnGUI()
    // {
    //     GUI.skin.label.fontSize = Screen.width / 100;
    //     GUI.skin.label.normal.textColor = Color.magenta;

    //     string ver = Application.version;
    //     GUILayout.Label(ver);
    //     GUILayout.Label($"Vibe Freq Default: {defaultVibrationFrequency}");
    //     GUILayout.Label($"Vibe Freq: {vibrationFrequency}");
    //     GUILayout.Label($"Vibe Amp Default: {defaultVibrationAmplitude}");        
    //     GUILayout.Label($"Vibe Amp: {vibrationAmplitude}");
    // }
}