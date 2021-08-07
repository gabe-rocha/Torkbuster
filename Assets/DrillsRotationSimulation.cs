using System.Diagnostics.Contracts;
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
    public float rpmChangeSpeed = 2f;
    public float torqueIncreaseWithShock = 50f;
    public float minRPM = 0f;
    public float maxRPM = 200f;
    public float torqueReductionDuration = 3f;
    public float rpmToTorqueFactor = 1.5f;
    public float vibrationFrequency = 0.1f;
    public float vibrationAmplitude = 0.0025f;
    public float vibrationShockFactor = 2f;
    public float stutteringChance = 0.3f;
    public float stutterDuration = 1f;
    public float stutterMinInterval = 2f;
    public float stutteringFactor = 2f;
    public float torkbusterVibeAndFreqReductionFactor = 0.5f;
    public Transform torkBuster;
    public TextMeshProUGUI textRPM, textStutter;
    public Color stutterColor;
    public Transform drillToBeReplaced;
    public Image torqueGauge;
    public Transform torqueGaugeArrow;
    public Button rpmUp, rpmDown;
    public Light sceneLight;
    public Gradient minMaxGaugeColor;
    public Image shockFeedbackImageBad, shockFeedbackImageGood;
    public float shockFeedbackDuration = 0.2f;
    public GameObjectPooler redParticlesPool, blueParticlesPool;
    

    private bool isTorkbusterInstalled = false;
    private int indexDrillReplacement = 2;

    private bool isStopped = false;
    private float zRotationEulersPerSecond = 0;
    private Coroutine shockCoRo, coroRemovingOldDrillOut, coroInstallingOldDrill, coroInstallingTorkBuster, coroRemovingTorkBuster, changeRPMCoro, badShockFeedback, goodShockFeedback;
    private bool isShocking = false;
    private float oldTorque;
    // private Vector3 drillInitialPos;
    private bool isDrillMovingUp = true;
    private Vector3 targetPosUp, targetPosDown;
    private float defaultVibrationFrequency, defaultVibrationAmplitude;
    private bool isStuttering = false;
    private float stutterStartTime, lastStutterDraw;
    private float maxFreq, maxAmp;
    private float rpmToGaugeArrowRotationfactor, currentTorque;    
    private float targetRPM;    


    private void Start()
    {
        
        targetPosUp = Vector3.up * vibrationAmplitude;
        targetPosDown = -1 * Vector3.up * vibrationAmplitude;
        
        currentTorque = currentRPM * rpmToTorqueFactor;
        zRotationEulersPerSecond = 360f * currentRPM / 60f;

        oldTorque = currentTorque;
        targetRPM = currentRPM;

        defaultVibrationFrequency = vibrationFrequency;
        defaultVibrationAmplitude = vibrationAmplitude;

        maxFreq = defaultVibrationFrequency * 3f;
        maxAmp = defaultVibrationAmplitude * 3f;

        // rpmSlider.value = currentRPM;
        // rpmSlider.onValueChanged.AddListener (delegate {RPMSliderValueChanged ();});
        
        rpmToGaugeArrowRotationfactor = -144.5f / maxRPM; //200 rpm is 144.5 degrees
        shockFeedbackImageBad.enabled = false;
        shockFeedbackImageGood.enabled = false;
        shockFeedbackImageBad.CrossFadeAlpha(0,0, true);
        shockFeedbackImageGood.CrossFadeAlpha(0,0, true);


        UpdateUI();
    }

    // public void RPMSliderValueChanged()
	// {
    //     if(isShocking){
    //         return;
    //     }
	// 	currentRPM = rpmSlider.value;
    //     oldRPM = currentRPM;
    //     zRotationEulersPerSecond = 360f * currentRPM / 60f;
    //     UpdateUI();
	// }

    private void Update()
    {
        RotateDrillsContinuously();
        VibrateDrillsContinuously();
        StutterDrills();
        UpdateTorqueGauge();
        // UpdateLight();
    }

    private void UpdateLight()
    {
        sceneLight.color = minMaxGaugeColor.Evaluate(currentRPM / maxRPM);
    }

    private void RotateDrillsContinuously()
    {
        transform.Rotate(Vector3.forward * zRotationEulersPerSecond * Time.deltaTime * -1, Space.Self);
        // torkBuster.Rotate(-1f * Vector3.right * zRotationEulersPerSecond * Time.deltaTime, Space.Self);
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
        targetPosUp = Vector3.up * vibrationAmplitude * targetRPM;
        targetPosDown = -1 * Vector3.up * vibrationAmplitude * targetRPM;

        if(isDrillMovingUp)
        {
            if(transform.position.y < targetPosUp.y){
                targetPosUp.x = transform.position.x;
                targetPosUp.z = transform.position.z;
                transform.position = Vector3.MoveTowards(transform.position, targetPosUp, vibrationFrequency * targetRPM * Time.deltaTime);
                
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
                transform.position = Vector3.MoveTowards(transform.position, targetPosDown, vibrationFrequency * targetRPM * Time.deltaTime);

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

            foreach(MeshRenderer mesh in drillToBeReplaced.gameObject.GetComponentsInChildren<MeshRenderer>()){
                mesh.enabled = false;    
            }
            torkBuster.gameObject.SetActive(true);
            isStuttering = false;
            UpdateUI();
        }
        else{
            vibrationAmplitude = defaultVibrationAmplitude;
            vibrationFrequency = defaultVibrationFrequency;
            foreach(MeshRenderer mesh in drillToBeReplaced.gameObject.GetComponentsInChildren<MeshRenderer>()){
                mesh.enabled = true;    
            }
            torkBuster.gameObject.SetActive(false);
            UpdateUI();
        }
        
    }

    public void OnButtonIncreaseRPMPressed(){
        if(isShocking) return;

        IncreaseRPM();
    }

    public void OnButtonDecreaseRPMPressed(){
        if(isShocking) return;

        DecreaseRPM();
    }

    private void IncreaseRPM(){
        if(targetRPM < maxRPM){
            targetRPM += rpmIncreaseStep;
            
            if(changeRPMCoro != null){
                StopCoroutine(changeRPMCoro);
            }
            
            changeRPMCoro = StartCoroutine(SlowlyIncreaseRPM());
        }
    }

    private IEnumerator SlowlyIncreaseRPM()
    {
        // oldRPM = currentRPM;
        while(currentRPM < targetRPM){
            currentRPM += rpmIncreaseStep * rpmChangeSpeed * Time.deltaTime;
            currentTorque = currentRPM * rpmToTorqueFactor;
            Mathf.Clamp(currentRPM, minRPM, maxRPM);
            zRotationEulersPerSecond = 360f * currentRPM / 60f;
            oldTorque = currentTorque;

            UpdateUI();
            yield return null;
        }
    }

    private void DecreaseRPM(){
        if(targetRPM > minRPM){
            targetRPM -= rpmIncreaseStep;

            if(changeRPMCoro != null){
                StopCoroutine(changeRPMCoro);
            }
            
            changeRPMCoro = StartCoroutine(SlowlyDecreaseRPM());
        }
    }

    private IEnumerator SlowlyDecreaseRPM()
    {
        // oldRPM = currentRPM;
        while(currentRPM > targetRPM){
            currentRPM -= rpmIncreaseStep * rpmChangeSpeed * Time.deltaTime;
            currentTorque = currentRPM * rpmToTorqueFactor;
            Mathf.Clamp(currentRPM, minRPM, maxRPM);
            zRotationEulersPerSecond = 360f * currentRPM / 60f;
            oldTorque = currentTorque;

            UpdateUI();
            yield return null;
        }
    }

    private void IncreaseTorqueAndVibeDueToShock(){
        if(currentTorque < maxRPM * rpmToTorqueFactor){
            currentTorque += torqueIncreaseWithShock;
            // currentRPM = Mathf.Clamp(currentRPM, minRPM, maxRPM);
            // zRotationEulersPerSecond = 360f * currentTorque / 60f;

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
            shockCoRo = StartCoroutine(TorqueAndVibeReductionAfterSeconds());
        }
    }

    IEnumerator TorqueAndVibeReductionAfterSeconds(){
        float startTime = Time.time;
        float deltaTorque = currentTorque - oldTorque;
        
        float deltaVibeFreq, deltaVibeAmp;
        if(isTorkbusterInstalled){
            deltaVibeFreq = vibrationFrequency - (defaultVibrationFrequency * torkbusterVibeAndFreqReductionFactor);
            deltaVibeAmp = vibrationAmplitude - (defaultVibrationAmplitude * torkbusterVibeAndFreqReductionFactor);
        }
        else{            
            deltaVibeFreq = vibrationFrequency - defaultVibrationFrequency;
            deltaVibeAmp = vibrationAmplitude - defaultVibrationAmplitude;
        }

        while(Time.time < startTime + torqueReductionDuration){
            currentTorque -= deltaTorque / torqueReductionDuration * Time.deltaTime;
            // zRotationEulersPerSecond = 360f * currentTorque / 60f;
            vibrationFrequency -= deltaVibeFreq / torqueReductionDuration * Time.deltaTime;
            vibrationAmplitude -= deltaVibeAmp / torqueReductionDuration * Time.deltaTime;
            
            UpdateUI();
            yield return null;
        }
        currentTorque = oldTorque;
        // zRotationEulersPerSecond = 360f * currentTorque / 60f;

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

        GameObject particleGO;
        bool isRed = false;
        float timeToLive = 5f;

        if(isTorkbusterInstalled){
            particleGO = blueParticlesPool.GetAPooledObject();
            isRed = false;
            if(goodShockFeedback != null)
            {
                StopCoroutine(goodShockFeedback);
            }
            goodShockFeedback = StartCoroutine(ShowGoodShockFeedFlash());
        }
        else{
            particleGO = redParticlesPool.GetAPooledObject();
            isRed = true;
            if(badShockFeedback != null)
            {
                StopCoroutine(badShockFeedback);
            }
            badShockFeedback = StartCoroutine(ShowBadShockFeedFlash());
        }

        particleGO.GetComponent<ParticleSystem>().Play();
        StartCoroutine(ReturnParticleSystemToPoolAfterSeconds(particleGO, isRed, timeToLive));

        IncreaseTorqueAndVibeDueToShock();        
    }

    private IEnumerator ReturnParticleSystemToPoolAfterSeconds(GameObject go, bool isRed, float timeToLive)
    {
        yield return new WaitForSeconds(timeToLive);
        if(isRed){
            redParticlesPool.ReturnUsedObject(go);            
        }
        else{
            blueParticlesPool.ReturnUsedObject(go);
        }

    }

    private IEnumerator ShowBadShockFeedFlash()
    {
        shockFeedbackImageBad.enabled = true;

        float fadeInMultiplier = 0.25f;
        float fadeOutMultiplier = 0.5f;

        yield return new WaitForSeconds(0.25f);
        float startTime = Time.time;
        while(Time.time < startTime + shockFeedbackDuration * fadeInMultiplier)
        {
            shockFeedbackImageBad.CrossFadeAlpha(1, shockFeedbackDuration *fadeInMultiplier, true);
            yield return null;
        }
        
        startTime = Time.time;
        while(Time.time < startTime + shockFeedbackDuration * fadeOutMultiplier)
        {
            shockFeedbackImageBad.CrossFadeAlpha(0, shockFeedbackDuration * fadeOutMultiplier, true);
            yield return null;
        }
        shockFeedbackImageBad.enabled = false;
    }
    private IEnumerator ShowGoodShockFeedFlash()
    {
        shockFeedbackImageGood.enabled = true;
        // shockFeedbackImageGood.CrossFadeAlpha(0,0, true);

        float fadeInMultiplier = 0.25f;
        float fadeOutMultiplier = 0.5f;

        yield return new WaitForSeconds(0.25f);
        float startTime = Time.time;
        while(Time.time < startTime + shockFeedbackDuration * fadeInMultiplier)
        {
            shockFeedbackImageGood.CrossFadeAlpha(1, shockFeedbackDuration * fadeInMultiplier, true);
            yield return null;
        }
        
        startTime = Time.time;
        while(Time.time < startTime + shockFeedbackDuration * fadeOutMultiplier)
        {
            shockFeedbackImageGood.CrossFadeAlpha(0, shockFeedbackDuration * fadeOutMultiplier, true);
            yield return null;
        }
        shockFeedbackImageGood.enabled = false;
    }

    IEnumerator RemoveTorkBuster(){
        yield return null;
    }
    IEnumerator InstallTorkBuster(){
        yield return null;
    }

    private void UpdateTorqueGauge(){
        // var defaultRotationZ = currentRPM * rpmToGaugeArrowRotationfactor;
        torqueGaugeArrow.localRotation = Quaternion.Euler(0f,0f, currentTorque * rpmToGaugeArrowRotationfactor + Random.Range(-currentTorque/100f, currentTorque/100f));
        torqueGauge.color = minMaxGaugeColor.Evaluate(currentRPM / maxRPM);
    }

    private void UpdateUI(){
        textRPM.text = currentRPM.ToString($"000") + " RPM";
        textStutter.color = isStuttering ? stutterColor : Color.gray;

        rpmUp.interactable = !isShocking;
        rpmDown.interactable = !isShocking;

        // rpmSlider.value = currentRPM;

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