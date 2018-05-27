using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jazornowsky.QuantumStorage.model;
using Jazornowsky.QuantumStorage.service;
using Jazornowsky.QuantumStorage.utils;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Jazornowsky.QuantumStorage
{
    abstract class AbstractQuantumIoMachine : MachineEntity, IQuantumIo
    {
        protected readonly MachineSides MachineSides = new MachineSides();
        protected readonly StorageIoService StorageIoService;
        protected long[] ControllerPos = new long[3];
        private bool _hooverInitialised;
        private bool _objectColorInitialised;

        protected AbstractQuantumIoMachine(MachineEntityCreationParameters parameters) : base(parameters)
        {
            mbNeedsLowFrequencyUpdate = true;
            mbNeedsUnityUpdate = true;
            PositionUtils.SetupSidesPositions(parameters.Flags, MachineSides);
            StorageIoService = new StorageIoService(this, MachineSides);
        }

        public override void OnUpdateRotation(byte newFlags)
        {
            base.OnUpdateRotation(newFlags);
            PositionUtils.SetupSidesPositions(newFlags, MachineSides);
        }

        public long[] GetControllerPos()
        {
            return ControllerPos;
        }

        public void SetControllerPos(long x, long y, long z)
        {
            ControllerPos[0] = x;
            ControllerPos[1] = y;
            ControllerPos[2] = z;
        }

        public override void UnityUpdate()
        {
            UpdateWorkLight();
            InitHoover();
            InitColor();
        }

        private void UpdateWorkLight()
        {
            if (!GameObjectsInitialized())
            {
                return;
            }
            Light light = mWrapper.mGameObjectList[0].transform.Search("HooverGraphic").GetComponent<Light>();
            var maxLightDistance = 64f;

            bool flag = !mbWellBehindPlayer;

            if (mDistanceToPlayer > (double) maxLightDistance)
            {
                flag = false;
            }
            if (flag)
            {
                if (!light.enabled)
                {
                    light.enabled = true;
                    light.range = 0.05f;
                }
                light.color = Color.Lerp(light.color, Color.magenta, Time.deltaTime);
                light.range += 0.1f;

                if (light.range > 1.0)
                {
                    light.range = 1f;
                }
            }

            if (!light.enabled)
            {
                return;
            }
            if (light.range < 0.150000005960464)
            {
                light.enabled = false;
            }
            else
            {
                light.range *= 0.95f;
            }
        }

        private void InitHoover()
        {
            if (!GameObjectsInitialized() || _hooverInitialised)
            {
                return;
            }
            var hooverPart = mWrapper.mGameObjectList[0].transform.Search("HooverGraphic")
                .GetComponent<ParticleSystem>();
            hooverPart.SetEmissionRate(0.0f);
            _hooverInitialised = true;
        }

        private void InitColor()
        {
            if (!GameObjectsInitialized() || _objectColorInitialised)
            {
                return;
            }
            var hopperPart = mWrapper.mGameObjectList[0].transform.Find("Hopper").gameObject;
            MeshRenderer Render2 = hopperPart.GetComponent<MeshRenderer>();
            Render2.material.SetColor("_Color", Color.magenta);
            _objectColorInitialised = true;
        }

        protected bool GameObjectsInitialized()
        {
            return mWrapper?.mGameObjectList != null && 
                   mWrapper.mGameObjectList.Count > 0;
        }

        public override HoloMachineEntity CreateHolobaseEntity(Holobase holobase)
        {
            HolobaseEntityCreationParameters parameters = new HolobaseEntityCreationParameters(this);
            parameters.AddVisualisation(holobase.mPreviewCube).Color = Color.yellow;
            return holobase.CreateHolobaseEntity(parameters);
        }
    }
}