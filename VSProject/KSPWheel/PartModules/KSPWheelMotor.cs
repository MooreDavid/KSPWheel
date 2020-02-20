﻿using System;
using System.Collections.Generic;
using UnityEngine;

namespace KSPWheel
{
    /// <summary>
    /// Replacement for stock wheel motor module.<para/>
    /// Manages wheel motor input and resource use.
    /// </summary>
    public class KSPWheelMotor : KSPWheelSubmodule, IContractObjectiveModule
    {
        
        /// <summary>
        /// Peak Motor power, in kw (e.g. kn).  Used to determine EC/s
        /// </summary>
        [KSPField]
        public float motorEfficiency = 0.85f;

        /// <summary>
        /// This is the ratio of no-load power draw to max-torque power draw.  E.g. at 0.05, if a motor draws 1kw at stall, it will draw 0.05kw at no-load max rpm.
        /// This value determines the location of the efficiency peak; closer to zero and the peak approaches max RPM, closer to 0.50 and the peak approaches 50% max RPM.
        /// </summary>
        [KSPField]
        public float motorPowerFactor = 0.05f;

        /// <summary>
        /// Motor stall torque; e.g. motor torque output at zero rpm
        /// </summary>
        [KSPField]
        public float maxMotorTorque = 10f;

        /// <summary>
        /// Max rpm of the motor at shaft.  Used with motor stall torque to determine output curve and power use.
        /// </summary>
        [KSPField]
        public float maxRPM = 2500f;

        /// <summary>
        /// User-selectable motor output limiter
        /// </summary>
        [KSPField(guiName = "Motor Limit", guiActive = true, guiActiveEditor = true, isPersistant = true, guiUnits ="%"),
         UI_FloatRange(minValue = 0f, maxValue = 100f, stepIncrement = 0.5f)]
        public float motorOutput = 100f;

        /// <summary>
        /// If true, motor response will be inverted for this wheel.  Toggleable in editor and flight.  Persistent.
        /// </summary>
        [KSPField(guiName = "Invert Motor", guiActive = true, guiActiveEditor = true, isPersistant = true),
         UI_Toggle(enabledText = "Inverted", disabledText = "Normal", suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.None)]
        public bool invertMotor;

        /// <summary>
        /// If true, motor response will be inverted for this wheel.  Toggleable in editor and flight.  Persistent.
        /// </summary>
        [KSPField(guiName = "Motor Lock", guiActive = true, guiActiveEditor = true, isPersistant = true),
         UI_Toggle(enabledText = "Locked", disabledText = "Free", suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.Editor)]
        public bool motorLocked;

        /// <summary>
        /// Tank-steering main toggle; only configurable through config.
        /// </summary>
        [KSPField]
        public bool tankSteering = false;

        [KSPField(guiName = "Tank Steer Invert", guiActive = false, guiActiveEditor = false, isPersistant = true),
         UI_Toggle(enabledText = "Inverted", disabledText = "Normal", suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.None)]
        public bool invertSteering = false;

        [KSPField(guiName = "Tank Steer Lock", guiActive = false, guiActiveEditor = false, isPersistant = true),
         UI_Toggle(enabledText = "Locked", disabledText = "Free", suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.None)]
        public bool steeringLocked = false;

        [KSPField(guiName = "Half-Track", guiActive = false, guiActiveEditor = false, isPersistant = true),
         UI_Toggle(enabledText = "Enabled", disabledText = "Disabled", suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.None)]
        public bool halfTrackSteering = false;

        [KSPField(guiName = "Gear Ratio (x:1)", guiActive = true, guiActiveEditor = true, isPersistant = true),
         UI_FloatEdit(suppressEditorShipModified = true, minValue = 0.25f, maxValue = 20f, incrementSlide = 0.05f, incrementLarge = 1f, incrementSmall = 0.25f, sigFigs = 2)]
        public float gearRatio = 4f;

        [KSPField]
        public float minGearRatio = 0.25f;

        [KSPField]
        public float maxGearRatio = 20f;

        /// <summary>
        /// GUI display values below here
        /// </summary>
        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "Max Drive Speed", guiUnits = "m/s")]
        public float maxDrivenSpeed = 0f;

        [KSPField(guiActive = true, guiActiveEditor = false, guiName = "Motor RPM")]
        public float motorCurRPM = 0f;

        [KSPField(guiActive = true, guiActiveEditor = false, guiName = "Torque To Wheel", guiUnits = "kN/M")]
        public float torqueOut = 0f;

        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "Mech. Output", guiUnits = "kW")]
        public float powerOutKW = 0f;

        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "Elec. Input", guiUnits = "kW")]
        public float powerInKW = 0f;

        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "Efficiency", guiUnits = "%")]
        public float powerEff = 0f;

        [KSPField(guiActive = true, guiName = "Motor EC Use", guiUnits = "ec/s")]
        public float guiResourceUse = 0f;

        [KSPField]
        public bool useTorqueCurve = true;

        /// <summary>
        /// Should this part auto-invert the steering and motor setups when used in symmetry in the editor?  Disable for parts with properly setup left/right counterparts.
        /// </summary>
        [KSPField]
        public bool invertMirror = true;

        /// <summary>
        /// The motor output torque curve.  Defaults to linear (ideal brushless DC motor).
        /// </summary>
        [KSPField]
        public FloatCurve torqueCurve = new FloatCurve();

        [KSPField(guiName = "Max EC/s")]
        public float maxECDraw = 0f;

        [KSPField]
        public bool showGUIMotorLimit = true;
        [KSPField]
        public bool showGUIMotorInvert = true;
        [KSPField]
        public bool showGUIMotorLock = true;
        [KSPField]
        public bool showGUISteerLock = true;
        [KSPField]
        public bool showGUISteerInvert = true;
        [KSPField]
        public bool showGUIGearRatio = true;
        [KSPField]
        public bool showGUIHalfTrack = true;
        [KSPField]
        public bool showGUIStats = true;

        public float torqueOutput;
        private float scaledMaxTorque = 0f;//actual post-scaling max torque
        private float scaledMaxRPM = 0f;
        private float peakInputPower = 0f;
        private float minInputPower = 0f;
        private float powerConversion = 65f;

        private static float rpmToRad = 0.104719755f;
        private static float radToRPM = 1 / 0.104719755f;

        public void onMotorInvert(BaseField field, System.Object obj)
        {
            if (HighLogic.LoadedSceneIsEditor && part.symmetryCounterparts.Count == 1)
            {
                part.symmetryCounterparts[0].GetComponent<KSPWheelMotor>().invertMotor = !invertMotor;
            }
            else if (HighLogic.LoadedSceneIsFlight)
            {
                this.wheelGroupUpdate(int.Parse(controller.wheelGroup), m =>
                {
                    m.invertMotor = invertMotor;
                });
            }
        }

        public void onGearUpdated(BaseField field, System.Object ob)
        {
            this.wheelGroupUpdate(int.Parse(controller.wheelGroup), m =>
            {
                m.gearRatio = gearRatio;
                m.calcPowerStats();
                m.updateUIFloatEditControl(nameof(m.gearRatio), m.gearRatio);
            });
        }

        internal void onMotorLock(BaseField field, System.Object obj)
        {
            this.wheelGroupUpdate(int.Parse(controller.wheelGroup), m =>
            {
                m.motorLocked = motorLocked;
            });
        }

        internal void onSteeringLock(BaseField field, System.Object obj)
        {
            this.wheelGroupUpdate(int.Parse(controller.wheelGroup), m =>
            {
                m.steeringLocked = steeringLocked;
            });
        }

        internal void onSteeringInvert(BaseField field, System.Object obj)
        {
            this.wheelGroupUpdate(int.Parse(controller.wheelGroup), m =>
            {
                m.invertSteering = invertSteering;
            });
        }

        internal void onMotorLimitUpdated(BaseField field, System.Object obj)
        {
            this.wheelGroupUpdate(int.Parse(controller.wheelGroup), m =>
            {
                m.motorOutput = motorOutput;
            });
        }

        internal void onHalftrackToggle(BaseField field, System.Object obj)
        {
            this.wheelGroupUpdate(int.Parse(controller.wheelGroup), m =>
            {
                m.halfTrackSteering = halfTrackSteering;
            });
        }

        [KSPAction("Lock Motor")]
        public void motorLockAction(KSPActionParam param)
        {
            motorLocked = !motorLocked;
        }

        [KSPAction("Invert Motor")]
        public void motorInvertAction(KSPActionParam param)
        {
            invertMotor = !invertMotor;
        }

        [KSPAction("Lock Steering")]
        public void steeringLockAction(KSPActionParam param)
        {
            steeringLocked = !steeringLocked;
        }

        [KSPAction("Invert Steering")]
        public void steeringInvertAction(KSPActionParam param)
        {
            invertSteering = !invertSteering;
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            Fields[nameof(invertMotor)].uiControlEditor.onFieldChanged = Fields[nameof(invertMotor)].uiControlFlight.onFieldChanged = onMotorInvert;
            Fields[nameof(gearRatio)].uiControlEditor.onFieldChanged = Fields[nameof(gearRatio)].uiControlFlight.onFieldChanged = onGearUpdated;
            Fields[nameof(motorLocked)].uiControlEditor.onFieldChanged = Fields[nameof(motorLocked)].uiControlFlight.onFieldChanged = onMotorLock;
            Fields[nameof(steeringLocked)].uiControlEditor.onFieldChanged = Fields[nameof(steeringLocked)].uiControlFlight.onFieldChanged = onSteeringLock;
            Fields[nameof(invertSteering)].uiControlEditor.onFieldChanged = Fields[nameof(invertSteering)].uiControlFlight.onFieldChanged = onSteeringInvert;
            Fields[nameof(motorOutput)].uiControlEditor.onFieldChanged = Fields[nameof(motorOutput)].uiControlFlight.onFieldChanged = onMotorLimitUpdated;
            Fields[nameof(halfTrackSteering)].uiControlEditor.onFieldChanged = Fields[nameof(halfTrackSteering)].uiControlFlight.onFieldChanged = onHalftrackToggle;
            if (torqueCurve.Curve.length == 0)
            {
                torqueCurve.Add(0, 1, -1, -1);
                torqueCurve.Add(1, 0, -1, -1);
            }
            if (HighLogic.LoadedSceneIsEditor && part.isClone && part.symmetryCounterparts != null && part.symmetryCounterparts.Count > 0)
            {
                if (invertMirror)
                {
                    invertMotor = !part.symmetryCounterparts[0].GetComponent<KSPWheelMotor>().invertMotor;
                }
                else
                {
                    invertSteering = !part.symmetryCounterparts[0].GetComponent<KSPWheelMotor>().invertSteering;
                }
            }
            ConfigNode config = GameDatabase.Instance.GetConfigNodes("KSPWHEELCONFIG")[0];
            powerConversion = config.GetFloatValue("powerConversion", 65f);
            calcPowerStats();
            Actions[nameof(steeringLockAction)].active = tankSteering && showGUISteerLock;
            Actions[nameof(steeringInvertAction)].active = tankSteering && showGUISteerInvert;

            UI_FloatEdit fe = (UI_FloatEdit)(HighLogic.LoadedSceneIsEditor ? Fields[nameof(gearRatio)].uiControlEditor : Fields[nameof(gearRatio)].uiControlFlight);
            fe.minValue = minGearRatio;
            fe.maxValue = maxGearRatio;
        }

        public string GetContractObjectiveType()
        {
            return "Wheel";
        }

        public bool CheckContractObjectiveValidity()
        {
            return true;
        }

        internal override string getModuleInfo()
        {
            string val = "Motor Torque: " + maxMotorTorque + "\n" + "Motor Max RPM: " + maxRPM + "\n" + "Motor Efficiency: "+motorEfficiency;
            if (tankSteering)
            {
                val = val + "\n" + "Tank Steering Enabled";
            }
            return val;
        }

        internal override void onScaleUpdated()
        {
            base.onScaleUpdated();
            calcPowerStats();
        }

        internal override void onUIControlsUpdated(bool show)
        {
            base.onUIControlsUpdated(show);

            Fields[nameof(motorOutput)].guiActive = Fields[nameof(motorOutput)].guiActiveEditor = show && showGUIMotorLimit;
            Fields[nameof(invertMotor)].guiActive = Fields[nameof(invertMotor)].guiActiveEditor = show && showGUIMotorInvert;
            Fields[nameof(motorLocked)].guiActive = Fields[nameof(motorLocked)].guiActiveEditor = show && showGUIMotorLock;

            Fields[nameof(invertSteering)].guiActive = Fields[nameof(invertSteering)].guiActiveEditor = tankSteering && show && showGUISteerInvert;
            Fields[nameof(steeringLocked)].guiActive = Fields[nameof(steeringLocked)].guiActiveEditor = tankSteering && show && showGUISteerLock;
            Fields[nameof(halfTrackSteering)].guiActive = Fields[nameof(halfTrackSteering)].guiActiveEditor = tankSteering && show && showGUIHalfTrack;

            Fields[nameof(gearRatio)].guiActive = Fields[nameof(gearRatio)].guiActiveEditor = show && HighLogic.CurrentGame.Parameters.CustomParams<KSPWheelSettings>().manualGearing && showGUIGearRatio;

            Fields[nameof(maxDrivenSpeed)].guiActive = Fields[nameof(maxDrivenSpeed)].guiActiveEditor = show && showGUIStats;
            Fields[nameof(motorCurRPM)].guiActive = Fields[nameof(motorCurRPM)].guiActiveEditor = show && showGUIStats;
            Fields[nameof(torqueOut)].guiActive = Fields[nameof(torqueOut)].guiActiveEditor = show && showGUIStats;
            Fields[nameof(powerOutKW)].guiActive = Fields[nameof(powerOutKW)].guiActiveEditor = show && showGUIStats;
            Fields[nameof(powerInKW)].guiActive = Fields[nameof(powerInKW)].guiActiveEditor = show && showGUIStats;
            Fields[nameof(powerEff)].guiActive = Fields[nameof(powerEff)].guiActiveEditor = show && showGUIStats;
            Fields[nameof(guiResourceUse)].guiActive = Fields[nameof(guiResourceUse)].guiActiveEditor = show && showGUIStats;
            Fields[nameof(maxECDraw)].guiActiveEditor = show && showGUIStats;
        }

        internal override void preWheelPhysicsUpdate()
        {
            base.preWheelPhysicsUpdate();
            updateMotor();
        }

        protected virtual void updateMotor()
        {
            float fI = part.vessel.ctrlState.wheelThrottle + part.vessel.ctrlState.wheelThrottleTrim;
            if (motorLocked) { fI = 0; }
            if (invertMotor) { fI = -fI; }
            if (tankSteering && !steeringLocked && !motorLocked)
            {
                float rI = -(part.vessel.ctrlState.wheelSteer + part.vessel.ctrlState.wheelSteerTrim);
                if (invertSteering) { rI = -rI; }
                if (halfTrackSteering)
                {
                    bool spinningBackwards = false;
                    if ((fI < 0 && !invertMotor) || (fI > 0 && invertMotor) || spinningBackwards)
                    {
                        rI = -rI;
                    }
                }
                if (Mathf.Sign(wheel.rpm) !=Mathf.Sign(rI) && rI != 0)//if rI is commanding the wheel to slow down, also apply brakes, inversely proportional to 
                {
                    wheel.brakeTorque += (1 - torqueCurve.Evaluate(Mathf.Abs(wheel.rpm * gearRatio) / scaledMaxRPM)) * scaledMaxTorque * Mathf.Abs(rI);
                }
                fI += rI;
            }
            fI = Mathf.Clamp(fI, -1, 1);
            fI *= motorOutput * 0.01f;//motor limiting...
            float motorRPM = wheel.rpm * gearRatio;
            //integrateMotorEuler(fI, motorRPM);
            integrateMotorEulerSub(fI, motorRPM, 5);
            //integrateMotorRK4(fI, motorRPM, wheel.mass);

            motorCurRPM = Mathf.Abs(motorRPM);
            float torquePercent =  1 - (motorCurRPM / scaledMaxRPM);
            float rawTorqueOutput = scaledMaxTorque * torquePercent;
            powerOutKW = motorCurRPM * rawTorqueOutput * rpmToRad;
            //powerInKW = guiResourceUse * powerConversion; // this is the -actual- input power, but we need the pre-integration input power for the given RPM to derive efficiency
            powerInKW = minInputPower + torquePercent * (peakInputPower - minInputPower);//the actual calcualted input power for the output torque
            powerEff = (powerInKW <= 0 ? 0 : powerOutKW / powerInKW) * 100f;//finally, the efficiency of the raw output and input power values
        }

        protected void integrateMotorEuler(float fI, float motorRPM)
        {
            motorRPM = Mathf.Abs(motorRPM);
            float rawOutput = calcRawTorque(fI, motorRPM);
            float powerUse = calcECUse(Mathf.Abs(fI), motorRPM);
            rawOutput *= updateResourceDrain(powerUse);
            float gearedOutput = rawOutput * gearRatio;
            wheel.motorTorque = gearedOutput;
            torqueOutput = torqueOut = wheel.motorTorque;
        }

        /// <summary>
        /// Quick and semi-hacky sub-step integration for motor rpm/acceleration.
        /// Simulates accelerating the wheel with the motor torque (which is what happens in the wheel code currently)
        /// This -helps- to limit single-tick torques driving wheels past safe RPM values
        /// TODO -- unknown if the EC/s integration works properly....
        /// </summary>
        /// <param name="fI"></param>
        /// <param name="motorRPM"></param>
        /// <param name="substeps"></param>
        protected void integrateMotorEulerSub(float fI, float motorRPM, int substeps)
        {
            float p = 1.0f / substeps;
            float dt = Time.fixedDeltaTime * p;
            float ecs = 0f;
            float t = 0f;
            float tt = 0f;
            float rpm = motorRPM;
            for (int i = 0; i < substeps; i++)
            {
                tt = p * calcRawTorque(fI, rpm);
                t += tt;
                ecs += p * calcECUse(fI, rpm);
                rpm = wheelRPMIntegration(rpm, wheel.mass, tt, dt);
            }
            t *= updateResourceDrain(ecs);
            t *= gearRatio;
            wheel.motorTorque = t;
            torqueOutput = torqueOut = t;
        }

        protected float calcRawTorque(float fI, float motorRPM)
        {
            motorRPM = Mathf.Abs(motorRPM);
            float maxRPM = this.scaledMaxRPM;
            motorRPM = Mathf.Clamp(motorRPM, 0, maxRPM);
            float curveOut = torqueCurve.Evaluate(motorRPM / maxRPM);
            float outputTorque = curveOut * fI * controller.GetScaledMotorTorque(maxMotorTorque);
            return outputTorque;
        }

        protected float calcECUse(float fI, float motorRPM)
        {
            fI = Mathf.Abs(fI);
            if (fI <= 0) { return 0f; }
            motorRPM = Mathf.Abs(motorRPM);            
            if (motorRPM > scaledMaxRPM) { motorRPM = scaledMaxRPM; }
            float torquePercent = 1 - (motorRPM / scaledMaxRPM);
            float delta = peakInputPower - minInputPower;
            float powerDraw = torquePercent * delta + minInputPower;
            return (powerDraw * Mathf.Abs(fI)) / powerConversion;//65 is the stock electrical to mechanical conversion factor (1ec=1kj, but does the work of 65kj)
        }

        /// <summary>
        /// Seriously... don't ask where this math came from.... (days of spreadsheet work)
        /// </summary>
        internal void calcPowerStats()
        {
            //setup the scaled max values for torque and RPM based on the currently configured scale settings and current scale
            scaledMaxTorque = controller.GetScaledMotorTorque(maxMotorTorque);
            scaledMaxRPM = maxRPM * controller.motorMaxRPMScalingFactor;

            float radius = wheelData.scaledRadius(part.rescaleFactor * controller.scale);
            maxDrivenSpeed = radius * (scaledMaxRPM / gearRatio) * rpmToRad;
            calcPowerStats(scaledMaxTorque, scaledMaxRPM, motorEfficiency, motorPowerFactor, out peakInputPower, out minInputPower);            
            maxECDraw = peakInputPower / powerConversion;
        }

        public static void calcPowerStats(float maxTorque, float maxRPM, float efficiency, float powerFactor, out float maxKw, out float minKw)
        {
            /**

            The rpm% of peak efficiency is plotted as the intersection of two functions.

            The two equations are:
            a = powerFactor
            The slope of the line denoted by torque output, linear with slope of -1
            y = 1 - x

            The second is the curve denoted by the power equation and the power factor
            y = x * (-(1-a) * x + 1)

            The solution to those two equations (one of them) is:  (wolfram used to solve for equation =\)
            x = 1/(1-a) - sqrt( a / (a-1)^2 )

            Thus the rpm% where you will find the (config specified) peak efficiency is (x).
            
            From this you can calculate the mechanical output power at that
            rpm given the linear torque curve of the simulated motor type.
            outAtPeak = x * maxRPM * (1-x) * maxTorque * rpmToRadians

            From the output power and the specified efficiency the input
            power for that single point can be calculated.  
            inAtPeak = outAtPeak / efficiency
            
            As the input power 'curve' is actually a simple linear function, 
            the min and max input power values can both be derived from that
            single point.

            ??
            z = (1 - (1 - powerFactor)) * x
            inMax = inAtPeak / z
            inMin = inMax * powerFactor
            ??

            **/
            float efficiencyPeak = 1 / (1 - powerFactor) - Mathf.Sqrt(powerFactor / Mathf.Pow(powerFactor - 1, 2));
            float effInverse = 1 - efficiency;
            float kwOutputAtPeak = efficiencyPeak * maxRPM * effInverse * maxTorque * 0.10472f;
            float kwInputAtPeak = kwOutputAtPeak / efficiency;
            float peakInput = 1 / ((efficiencyPeak * powerFactor) + effInverse) * kwInputAtPeak;
            float noLoad = powerFactor * peakInput;
            maxKw = peakInput;
            minKw = noLoad;
        }

        protected float wheelRPMIntegration(float rpm, float wm, float torque, float deltaTime)
        {
            float wheelRPM = rpm / gearRatio;
            float wWheel = rpm * rpmToRad;
            float wAccel = torque / wm * deltaTime;
            wWheel += wAccel;
            return wWheel * radToRPM * gearRatio;
        }

        protected float updateResourceDrain(float ecs)
        {
            float percent = 1f;
            guiResourceUse = 0f;
            if (ecs > 0)
            {
                float drain = ecs * Time.fixedDeltaTime;
                if (drain > 0)
                {
                    float used = part.RequestResource("ElectricCharge", drain);
                    if (used != drain)
                    {
                        percent = used / drain;
                    }
                    guiResourceUse = percent * ecs;
                }
            }
            return percent;
        }

        ///// <summary>
        ///// RK4 integration for motor torque, to prevent wheel from spinning excessively
        ///// </summary>
        //protected void integrateMotorRK4(float fI, float rpm, float wheelMass)
        //{
        //    if (fI == 0)
        //    {
        //        wheel.motorTorque = torqueOut = torqueOutput = 0f;
        //        guiResourceUse = 0f;
        //        return;
        //    }
        //    //final outputs;
        //    float torque, ec;
        //    //initial state input
        //    float deltaTime = Time.fixedDeltaTime;
        //    //derivative outputs
        //    float ec1, ec2, ec3, ec4;
        //    float rpm1, rpm2, rpm3, rpm4;
        //    float t1, t2, t3, t4;
        //    //final outputs
        //    float ot;
        //    //float or;//not needed
        //    float oe;
        //    //derivative calcs
        //    motorDerivative(fI, wheelMass, deltaTime * 0.0f, rpm, 0, out t1, out rpm1, out ec1);
        //    motorDerivative(fI, wheelMass, deltaTime * 0.5f, rpm1, t1, out t2, out rpm2, out ec2);
        //    motorDerivative(fI, wheelMass, deltaTime * 0.5f, rpm2, t2, out t3, out rpm3, out ec3);
        //    motorDerivative(fI, wheelMass, deltaTime * 1.0f, rpm3, t3, out t4, out rpm4, out ec4);
        //    //derivative integration
        //    ot = 1.0f / 6.0f * (t1 + 2.0f * (t2 + t3) + t4);
        //    if (float.IsNaN(ot)) { ot = 0f; }//as /6*xxxxx can be /0...
        //    //or = 1.0f / 6.0f * (rpm1 + 2.0f * (rpm2 + rpm3) + rpm4);
        //    oe = 1.0f / 6.0f * (ec1 + 2.0f * (ec2 + ec3) + ec4);
        //    if (float.IsNaN(oe)) { oe = 0f; }
        //    torque = ot;
        //    ec = oe;

        //    torque *= updateResourceDrain(ec);
        //    wheel.motorTorque = torqueOutput = torqueOut = torque;
        //}

        //protected void motorDerivative(float fI, float wheelMass, float time, float dRpm, float dTorque, out float outTorque, out float outRpm, out float outECUse)
        //{
        //    outECUse = calcECUse(fI, dRpm);
        //    outTorque = calcRawTorque(fI, dRpm);
        //    outRpm = wheelRPMIntegration(dRpm, wheelMass, dTorque, time);
        //}

    }

}
