using System;
using System.Collections.Generic;
using com.hhotatea.avatar_pose_library.editor;
using UnityEngine;
using UnityEditor.Animations;
using com.hhotatea.avatar_pose_library.model;
using VRC.SDK3.Avatars.Components;
using VRC.SDKBase;

namespace com.hhotatea.avatar_pose_library.logic
{
    public static class AnimatorBuilder
    {
        public static AnimatorController BuildLocomotionAnimator(AvatarPoseData poseLibrary)
        {
            var result = BaseAnimator(poseLibrary);
            
            // レイヤー作成
            AnimatorControllerLayer layer = new AnimatorControllerLayer
            {
                name = $"{ConstVariables.AnimatorPrefix}_{poseLibrary.guid}",
                defaultWeight = 1f,
                stateMachine = new AnimatorStateMachine(),
                blendingMode = AnimatorLayerBlendingMode.Override,
            };
            result.AddLayer(layer);

            // 空のステート（default）
            var defaultState = layer.stateMachine.AddState("Default");
            defaultState.writeDefaultValues = true;
            defaultState.motion = new AnimationClip();
                
            // ポーズのレイヤー追加
            foreach (var category in poseLibrary.categories)
            {
                foreach (var pose in category.poses)
                {
                    AddLocomotionLayer(pose,layer,defaultState,poseLibrary.Parameters,
                        poseLibrary.guid,poseLibrary.enableHeightParam,poseLibrary.enableSpeedParam,poseLibrary.enableMirrorParam);
                }
            }

            return result;
        }
        public static AnimatorController BuildFxAnimator(AvatarPoseData poseLibrary)
        {
            var result = BaseAnimator(poseLibrary);
            
            // レイヤー作成
            AnimatorControllerLayer layer = new AnimatorControllerLayer
            {
                name = $"{ConstVariables.AnimatorPrefix}_{poseLibrary.guid}",
                defaultWeight = 1f,
                stateMachine = new AnimatorStateMachine(),
                blendingMode = AnimatorLayerBlendingMode.Override
            };
            result.AddLayer(layer);

            // 空のステート（default）
            var defaultState = layer.stateMachine.AddState("Default");
            defaultState.writeDefaultValues = true;
            defaultState.motion = new AnimationClip();
                
            // ポーズのレイヤー追加
            foreach (var category in poseLibrary.categories)
            {
                foreach (var pose in category.poses)
                {
                    AddFxLayer(pose,layer,defaultState,poseLibrary.Parameters,
                        poseLibrary.guid,poseLibrary.enableHeightParam,poseLibrary.enableSpeedParam,poseLibrary.enableMirrorParam);
                }
            }

            // その他の変数レイヤー
            result.AddLayer(TrackingLayer(TrackingType.Base,$"{ConstVariables.BaseParamPrefix}_{poseLibrary.guid}",poseLibrary));
            result.AddLayer(TrackingLayer(TrackingType.Head,$"{ConstVariables.HeadParamPrefix}_{poseLibrary.guid}",poseLibrary));
            result.AddLayer(TrackingLayer(TrackingType.Arm,$"{ConstVariables.ArmParamPrefix}_{poseLibrary.guid}",poseLibrary));
            result.AddLayer(TrackingLayer(TrackingType.Foot,$"{ConstVariables.FootParamPrefix}_{poseLibrary.guid}",poseLibrary));
            result.AddLayer(TrackingLayer(TrackingType.Finger,$"{ConstVariables.FingerParamPrefix}_{poseLibrary.guid}",poseLibrary));
            result.AddLayer(ResetLayer($"{ConstVariables.ResetParamPrefix}_{poseLibrary.guid}",poseLibrary));

            return result;
        }

        static AnimatorController BaseAnimator(AvatarPoseData poseLibrary)
        {
            var result = new AnimatorController();
            result.AddLayer(new AnimatorControllerLayer
            {
                name = "null",
                stateMachine = new AnimatorStateMachine(),
            });
            
            var heightParam = new AnimatorControllerParameter
            {
                name = $"{ConstVariables.HeightParamPrefix}_{poseLibrary.guid}",
                type = AnimatorControllerParameterType.Float,
                defaultFloat = 0.5f,
            };
            result.AddParameter(heightParam);
            
            var speedParam = new AnimatorControllerParameter
            {
                name = $"{ConstVariables.SpeedParamPrefix}_{poseLibrary.guid}",
                type = AnimatorControllerParameterType.Float,
                defaultFloat = 0.5f,
            };
            result.AddParameter(speedParam);
            
            var mirrorParam = new AnimatorControllerParameter
            {
                name = $"{ConstVariables.MirrorParamPrefix}_{poseLibrary.guid}",
                type = AnimatorControllerParameterType.Bool,
                defaultBool = false,
            };
            result.AddParameter(mirrorParam);
            
            foreach (var param in poseLibrary.Parameters)
            {
                // パラメーター追加
                result.AddParameter(param, AnimatorControllerParameterType.Int);
            }

            // Tracking制御ノード
            result.AddParameter($"{ConstVariables.BaseParamPrefix}_{poseLibrary.guid}", AnimatorControllerParameterType.Bool);
            result.AddParameter($"{ConstVariables.HeadParamPrefix}_{poseLibrary.guid}", AnimatorControllerParameterType.Bool);
            result.AddParameter($"{ConstVariables.ArmParamPrefix}_{poseLibrary.guid}", AnimatorControllerParameterType.Bool);
            result.AddParameter($"{ConstVariables.FootParamPrefix}_{poseLibrary.guid}", AnimatorControllerParameterType.Bool);
            result.AddParameter($"{ConstVariables.FingerParamPrefix}_{poseLibrary.guid}", AnimatorControllerParameterType.Bool);
            result.AddParameter($"{ConstVariables.ResetParamPrefix}_{poseLibrary.guid}", AnimatorControllerParameterType.Bool);

            return result;
        }

        static AnimatorControllerLayer ResetLayer(string param,AvatarPoseData poseLibrary)
        {
            // レイヤー作成
            AnimatorControllerLayer layer = new AnimatorControllerLayer
            {
                name = param,
                defaultWeight = 0f,
                stateMachine = new AnimatorStateMachine(),
                blendingMode = AnimatorLayerBlendingMode.Override
            };

            var noneClip = MotionBuilder.NoneAnimation();
            // ステートの初期化
            var defaultState = layer.stateMachine.AddState("Default");
            defaultState.writeDefaultValues = false;
            defaultState.motion = noneClip;
            
            var resetState = layer.stateMachine.AddState("Reset");
            resetState.writeDefaultValues = false;
            resetState.motion = noneClip;
            
            var paramReset = resetState.AddStateMachineBehaviour<VRCAvatarParameterDriver>();
            foreach (var parameter in poseLibrary.Parameters)
            {
                paramReset.parameters.Add(new VRC_AvatarParameterDriver.Parameter
                {
                    type = VRC_AvatarParameterDriver.ChangeType.Set,
                    name = parameter,
                    value = 0,
                });
            }
            foreach (var parameter in new String[]{
                         ConstVariables.HeadParamPrefix,
                         ConstVariables.ArmParamPrefix,
                         ConstVariables.FootParamPrefix,
                         ConstVariables.FingerParamPrefix,
                         ConstVariables.BaseParamPrefix,
                         ConstVariables.MirrorParamPrefix})
            {
                paramReset.parameters.Add(new VRC_AvatarParameterDriver.Parameter
                {
                    type = VRC_AvatarParameterDriver.ChangeType.Set,
                    name = $"{parameter}_{poseLibrary.guid}",
                    value = 0,
                });
            }
            foreach (var parameter in new String[]{
                         ConstVariables.SpeedParamPrefix,
                         ConstVariables.HeightParamPrefix,})
            {
                paramReset.parameters.Add(new VRC_AvatarParameterDriver.Parameter
                {
                    type = VRC_AvatarParameterDriver.ChangeType.Set,
                    name = $"{parameter}_{poseLibrary.guid}",
                    value = 0.5f,
                });
            }
            paramReset.parameters.Add(new VRC_AvatarParameterDriver.Parameter
            {
                type = VRC_AvatarParameterDriver.ChangeType.Set,
                name = param,
                value = 0,
            });
            
            // 遷移の設定
            var resetTransition = defaultState.AddTransition(resetState);
            resetTransition.canTransitionToSelf = false;
            resetTransition.hasExitTime = true;
            resetTransition.exitTime = 0f;
            resetTransition.hasFixedDuration = true;
            resetTransition.duration = 0.0f;
            resetTransition.conditions = new AnimatorCondition[]
            {
                new AnimatorCondition
                {
                    mode = AnimatorConditionMode.If,
                    parameter = param,
                }
            };
            
            var defaultTransition = resetState.AddTransition(defaultState);
            defaultTransition.canTransitionToSelf = false;
            defaultTransition.hasExitTime = true;
            defaultTransition.exitTime = 0f;
            defaultTransition.hasFixedDuration = true;
            defaultTransition.duration = 0.0f;

            return layer;
        }

        enum TrackingType
        {
            Base,
            Head,
            Arm,
            Foot,
            Finger
        }

        static AnimatorControllerLayer TrackingLayer(TrackingType type,string param,AvatarPoseData poseLibrary)
        {
            // レイヤー作成
            AnimatorControllerLayer layer = new AnimatorControllerLayer
            {
                name = param,
                defaultWeight = 0f,
                stateMachine = new AnimatorStateMachine(),
                blendingMode = AnimatorLayerBlendingMode.Override
            };

            var noneClip = MotionBuilder.NoneAnimation();
            
            // ステートの初期化
            var offIdleState = layer.stateMachine.AddState("OffIdle");
            offIdleState.writeDefaultValues = false;
            offIdleState.motion = noneClip;
            
            var offConState = layer.stateMachine.AddState("OffConState");
            offConState.writeDefaultValues = false;
            offConState.motion = noneClip;

            var onIdleState = layer.stateMachine.AddState("OnIdle");
            onIdleState.writeDefaultValues = false;
            onIdleState.motion = noneClip;
            
            var onConState = layer.stateMachine.AddState("OnConState");
            onConState.writeDefaultValues = false;
            onConState.motion = noneClip;
            
            // コンポーネント
            switch (type)
            {
                case TrackingType.Base:
                    var locoOn = offConState.AddStateMachineBehaviour<VRCAnimatorLocomotionControl>();
                    locoOn.disableLocomotion = false;
                    var locoOff = onConState.AddStateMachineBehaviour<VRCAnimatorLocomotionControl>();
                    locoOff.disableLocomotion = true;
                    ApplyTrackingLayer(offConState, onConState,
                        off => {
                            off.trackingHip = VRC_AnimatorTrackingControl.TrackingType.Tracking;
                        },
                        on => {
                            on.trackingHip = VRC_AnimatorTrackingControl.TrackingType.Animation;
                        });
                    break;

                case TrackingType.Head:
                    ApplyTrackingLayer(offConState, onConState,
                        off => off.trackingHead = VRC_AnimatorTrackingControl.TrackingType.Tracking,
                        on => on.trackingHead = VRC_AnimatorTrackingControl.TrackingType.Animation);
                    break;

                case TrackingType.Arm:
                    ApplyTrackingLayer(offConState, onConState,
                        off => {
                            off.trackingLeftHand = VRC_AnimatorTrackingControl.TrackingType.Tracking;
                            off.trackingRightHand = VRC_AnimatorTrackingControl.TrackingType.Tracking;
                        },
                        on => {
                            on.trackingLeftHand = VRC_AnimatorTrackingControl.TrackingType.Animation;
                            on.trackingRightHand = VRC_AnimatorTrackingControl.TrackingType.Animation;
                        });
                    break;

                case TrackingType.Foot:
                    ApplyTrackingLayer(offConState, onConState,
                        off => {
                            off.trackingLeftFoot = VRC_AnimatorTrackingControl.TrackingType.Tracking;
                            off.trackingRightFoot = VRC_AnimatorTrackingControl.TrackingType.Tracking;
                        },
                        on => {
                            on.trackingLeftFoot = VRC_AnimatorTrackingControl.TrackingType.Animation;
                            on.trackingRightFoot = VRC_AnimatorTrackingControl.TrackingType.Animation;
                        });
                    break;

                case TrackingType.Finger:
                    var gestureOff = offConState.AddStateMachineBehaviour<VRCPlayableLayerControl>();
                    gestureOff.layer = VRC_PlayableLayerControl.BlendableLayer.Gesture;
                    gestureOff.goalWeight = 1f;
                    var gestureOn = onConState.AddStateMachineBehaviour<VRCPlayableLayerControl>();
                    gestureOn.layer = VRC_PlayableLayerControl.BlendableLayer.Gesture;
                    gestureOn.goalWeight = 0f;
                    ApplyTrackingLayer(offConState, onConState,
                        off => {
                            off.trackingLeftFingers = VRC_AnimatorTrackingControl.TrackingType.Tracking;
                            off.trackingRightFingers = VRC_AnimatorTrackingControl.TrackingType.Tracking;
                        },
                        on => {
                            on.trackingLeftFingers = VRC_AnimatorTrackingControl.TrackingType.Animation;
                            on.trackingRightFingers = VRC_AnimatorTrackingControl.TrackingType.Animation;
                        });
                    break;
            }
            
            // 遷移の設定
            var fromOffToOn = offIdleState.AddTransition(onConState);
            fromOffToOn.canTransitionToSelf = false;
            fromOffToOn.hasExitTime = true;
            fromOffToOn.exitTime = 0f;
            fromOffToOn.hasFixedDuration = true;
            fromOffToOn.duration = 0.0f;
            fromOffToOn.conditions = new AnimatorCondition[]
            {
                new AnimatorCondition
                {
                    mode = AnimatorConditionMode.If,
                    parameter = param,
                }
            };
            
            var fromOnToOn = onConState.AddTransition(onIdleState);
            fromOnToOn.canTransitionToSelf = false;
            fromOnToOn.hasExitTime = true;
            fromOnToOn.exitTime = 0f;
            fromOnToOn.hasFixedDuration = true;
            fromOnToOn.duration = 0.0f;
            
            var fromOnToOff = onIdleState.AddTransition(offConState);
            fromOnToOff.canTransitionToSelf = false;
            fromOnToOff.hasExitTime = true;
            fromOnToOff.exitTime = 0f;
            fromOnToOff.hasFixedDuration = true;
            fromOnToOff.duration = 0.0f;
            fromOnToOff.conditions = new AnimatorCondition[]
            {
                new AnimatorCondition
                {
                    mode = AnimatorConditionMode.IfNot,
                    parameter = param,
                }
            };
            
            var fromOffToOff = offConState.AddTransition(offIdleState);
            fromOffToOff.canTransitionToSelf = false;
            fromOffToOff.hasExitTime = true;
            fromOffToOff.exitTime = 0f;
            fromOffToOff.hasFixedDuration = true;
            fromOffToOff.duration = 0.0f;
            
            // Off設定を維持する
            var loopTransition = onIdleState.AddTransition(onConState);
            loopTransition.canTransitionToSelf = false;
            loopTransition.hasExitTime = true;
            loopTransition.exitTime = 0f;
            loopTransition.hasFixedDuration = true;
            loopTransition.duration = 0.0f;

            return layer;
        }
        
        private static void ApplyTrackingLayer(
            AnimatorState offState, AnimatorState onState,
            Action<VRCAnimatorTrackingControl> configureOff,
            Action<VRCAnimatorTrackingControl> configureOn)
        {
            var offCon = offState.AddStateMachineBehaviour<VRCAnimatorTrackingControl>();
            var onCon = onState.AddStateMachineBehaviour<VRCAnimatorTrackingControl>();
            configureOff(offCon);
            configureOn(onCon);
        }

        private static void AddFxLayer(
            PoseEntry pose,
            AnimatorControllerLayer layer,
            AnimatorState defaultState,
            List<string> parameters,
            string guid, bool height, bool speed, bool mirror)
        {
            // トラッキング設定用のオブジェクト
            var trackingMap = new (bool enabled, string prefix)[]
            {
                (pose.tracking.head, ConstVariables.HeadParamPrefix),
                (pose.tracking.arm, ConstVariables.ArmParamPrefix),
                (pose.tracking.foot, ConstVariables.FootParamPrefix),
                (pose.tracking.finger, ConstVariables.FingerParamPrefix),
                (pose.tracking.locomotion, ConstVariables.BaseParamPrefix)
            };
            var noneClip = MotionBuilder.NoneAnimation();
            
            // 準備ステートの作成
            var reserveState = layer.stateMachine.AddState("Reserve_"+pose.value.ToString());
            reserveState.motion = noneClip;
            reserveState.writeDefaultValues = false;
            {
                var trackingOnParam = reserveState.AddStateMachineBehaviour<VRCAvatarParameterDriver>();
                foreach (var (enabled, prefix) in trackingMap)
                {
                    if (!enabled) continue;

                    trackingOnParam.parameters.Add(new VRC_AvatarParameterDriver.Parameter
                    {
                        type = VRC_AvatarParameterDriver.ChangeType.Set,
                        name = $"{prefix}_{guid}",
                        value = 1,
                    });
                }
                trackingOnParam.parameters.Add(new VRC_AvatarParameterDriver.Parameter
                {
                    type = VRC_AvatarParameterDriver.ChangeType.Set,
                    name = $"{ConstVariables.SpeedParamPrefix}_{guid}",
                    value = pose.tracking.motionSpeed == 0f ?  0f : 0.5f,
                });
                var additive = reserveState.AddStateMachineBehaviour<VRCPlayableLayerControl>();
                additive.layer = VRC_PlayableLayerControl.BlendableLayer.Additive;
                additive.goalWeight = 0f;
            }
            
            // メインステートの作成
            var poseState = layer.stateMachine.AddState("Pose_"+pose.value.ToString());
            poseState.writeDefaultValues = false;
            if (mirror)
            {
                poseState.mirrorParameterActive = true;
                poseState.mirrorParameter = $"{ConstVariables.MirrorParamPrefix}_{guid}";
            }

            // blendTree
            var anim = MotionBuilder.SetAnimationLoop(pose.animationClip,pose.tracking.loop);
            // Transform以外のAnimationを抽出
            poseState.motion = MotionBuilder.PartAnimation(anim)[1];
            if (MotionBuilder.IsMoveAnimation(anim))
            {
                // スピードを制御可能にする
                poseState.speed = pose.tracking.motionSpeed * 2f;
                poseState.speedParameterActive = true;
                poseState.speedParameter = $"{ConstVariables.SpeedParamPrefix}_{guid}";
            }

            // トラッキングリセット用のステート
            var resetState = layer.stateMachine.AddState("Reset"+pose.value.ToString());
            resetState.motion = noneClip;
            resetState.writeDefaultValues = false;
            {
                var trackingOffParam = resetState.AddStateMachineBehaviour<VRCAvatarParameterDriver>();
                foreach (var (enabled, prefix) in trackingMap)
                {
                    if (!enabled) continue;

                    trackingOffParam.parameters.Add(new VRC_AvatarParameterDriver.Parameter
                    {
                        type = VRC_AvatarParameterDriver.ChangeType.Set,
                        name = $"{prefix}_{guid}",
                        value = 0,
                    });
                }
            }
            {
                var additive = resetState.AddStateMachineBehaviour<VRCPlayableLayerControl>();
                additive.layer = VRC_PlayableLayerControl.BlendableLayer.Additive;
                additive.goalWeight = 1f;
            }
            
            // 変数リセット用のステート
            var preResetState = layer.stateMachine.AddState("PreReset"+pose.value.ToString());
            preResetState.motion = noneClip;
            preResetState.writeDefaultValues = false;
            var resetParam = preResetState.AddStateMachineBehaviour<VRCAvatarParameterDriver>();
            {
                resetParam.parameters.Add(new VRC_AvatarParameterDriver.Parameter
                {
                    type = VRC_AvatarParameterDriver.ChangeType.Set,
                    name = pose.parameter,
                    value = 0,
                });
            }
            
            // 遷移を作成
            var reTransition = defaultState.AddTransition(reserveState);
            reTransition.canTransitionToSelf = false;
            reTransition.hasExitTime = false;
            reTransition.hasFixedDuration = true;
            reTransition.duration = 0.0f;
            reTransition.conditions = new AnimatorCondition[]
            {
                new AnimatorCondition
                {
                    mode = AnimatorConditionMode.Equals,
                    parameter = pose.parameter,
                    threshold = pose.value
                }
            };
            
            // メインへの移行
            var mainTransition = reserveState.AddTransition(poseState);
            mainTransition.canTransitionToSelf = false;
            mainTransition.hasExitTime = true;
            mainTransition.exitTime = 0f;
            mainTransition.hasFixedDuration = true;
            mainTransition.duration = 0.0f;
            
            // Preからリセットへの遷移
            var bypassTransition = preResetState.AddTransition(resetState);
            bypassTransition.canTransitionToSelf = false;
            bypassTransition.hasExitTime = false;
            bypassTransition.hasFixedDuration = true;
            bypassTransition.duration = 0.0f;
            bypassTransition.conditions = new AnimatorCondition[]
            {
                new AnimatorCondition
                {
                    mode = AnimatorConditionMode.Equals,
                    parameter = pose.parameter,
                    threshold = 0
                }
            };
            
            // リセットへの遷移
            var resetTransition = poseState.AddTransition(resetState);
            resetTransition.canTransitionToSelf = false;
            resetTransition.hasExitTime = false;
            resetTransition.hasFixedDuration = true;
            resetTransition.duration = 0.0f;
            resetTransition.conditions = new AnimatorCondition[]
            {
                new AnimatorCondition
                {
                    mode = AnimatorConditionMode.NotEqual,
                    parameter = pose.parameter,
                    threshold = pose.value
                }
            };
            
            // パラメーターリストを作成
            parameters.Remove(pose.parameter);
            foreach (var p in parameters)
            {
                // プレリセットへの遷移
                var preResetTransition = poseState.AddTransition(preResetState);
                preResetTransition.canTransitionToSelf = false;
                preResetTransition.hasExitTime = false;
                preResetTransition.hasFixedDuration = true;
                preResetTransition.duration = 0.0f;
                preResetTransition.conditions = new AnimatorCondition[]
                {
                    new AnimatorCondition
                    {
                        mode = AnimatorConditionMode.NotEqual,
                        parameter = p,
                        threshold = 0
                    }
                };
            }
    
            // デフォルトへの遷移
            var defaultTransition = resetState.AddTransition(defaultState);
            defaultTransition.canTransitionToSelf = false;
            defaultTransition.hasExitTime = false;
            defaultTransition.hasFixedDuration = true;
            defaultTransition.duration = 0.0f;
            defaultTransition.conditions = new AnimatorCondition[]
            {
                new AnimatorCondition
                {
                    mode = AnimatorConditionMode.NotEqual,
                    parameter = pose.parameter,
                    threshold = pose.value
                }
            };

            if (!pose.tracking.loop)
            {
                var endTransition = poseState.AddTransition(preResetState);
                endTransition.canTransitionToSelf = false;
                endTransition.hasExitTime = true;
                endTransition.exitTime = 0f;
                endTransition.hasFixedDuration = true;
                endTransition.duration = 0.0f;
            }
        }

        private static void AddLocomotionLayer(
            PoseEntry pose,
            AnimatorControllerLayer layer,
            AnimatorState defaultState,
            List<string> parameters,
            string guid, bool height, bool speed, bool mirror)
        {
            var noneClip = MotionBuilder.NoneAnimation();
            
            // 準備ステートの作成
            var reserveState = layer.stateMachine.AddState("Reserve_"+pose.value.ToString());
            reserveState.motion = noneClip;
            reserveState.writeDefaultValues = false;
            
            // メインステートの作成
            var poseState = layer.stateMachine.AddState("Pose_"+pose.value.ToString());
            poseState.writeDefaultValues = false;
            if (mirror)
            {
                poseState.mirrorParameterActive = true;
                poseState.mirrorParameter = $"{ConstVariables.MirrorParamPrefix}_{guid}";
            }

            // blendTree
            var anim = MotionBuilder.SetAnimationLoop(pose.animationClip,pose.tracking.loop);
            // Transform関係のAnimation抽出
            anim = MotionBuilder.PartAnimation(anim)[0];
            if (MotionBuilder.IsMoveAnimation(anim))
            {
                var blendTree = new BlendTree();
                
                // アニメーションの生成
                AnimationClip motionClip0 = height ? MotionBuilder.BuildMotionLevel(anim, +DynamicVariables.Settings.minMaxHeight) : anim;
                AnimationClip motionClip1 = height ? MotionBuilder.BuildMotionLevel(anim, -DynamicVariables.Settings.minMaxHeight) : anim;
                blendTree.blendParameter = $"{ConstVariables.HeightParamPrefix}_{guid}";
                blendTree.AddChild(motionClip0, 0);
                blendTree.AddChild(motionClip1, 1);
                poseState.motion = blendTree;
                
                // スピードを制御可能にする
                poseState.speed = pose.tracking.motionSpeed * 2f;
                poseState.speedParameterActive = true;
                poseState.speedParameter = $"{ConstVariables.SpeedParamPrefix}_{guid}";
            }
            else
            {
                var blendTree = new BlendTree();
                var motionClip0 = speed ? MotionBuilder.IdleAnimation(anim,0f) : anim;
                var motionClip1 = speed ? MotionBuilder.IdleAnimation(anim,DynamicVariables.Settings.motionNoiseScale) : anim;
                var motionClip00 = height ? MotionBuilder.BuildMotionLevel(motionClip0,+DynamicVariables.Settings.minMaxHeight) : motionClip0;
                var motionClip01 = height ? MotionBuilder.BuildMotionLevel(motionClip0,-DynamicVariables.Settings.minMaxHeight) : motionClip0;
                var motionClip10 = height ? MotionBuilder.BuildMotionLevel(motionClip1,+DynamicVariables.Settings.minMaxHeight) : motionClip1;
                var motionClip11 = height ? MotionBuilder.BuildMotionLevel(motionClip1,-DynamicVariables.Settings.minMaxHeight) : motionClip1;
                blendTree.blendType = BlendTreeType.FreeformCartesian2D;
                blendTree.blendParameter = $"{ConstVariables.HeightParamPrefix}_{guid}";
                blendTree.blendParameterY = $"{ConstVariables.SpeedParamPrefix}_{guid}";
                blendTree.AddChild(motionClip00 , new Vector2(0f,0f));
                blendTree.AddChild(motionClip01, new Vector2(1f,0f));
                blendTree.AddChild(motionClip10, new Vector2(0f,1f));
                blendTree.AddChild(motionClip11, new Vector2(1f,1f));
                poseState.motion = blendTree;
            }
            
            // 遷移を作成
            var reTransition = defaultState.AddTransition(poseState);
            reTransition.canTransitionToSelf = false;
            reTransition.hasExitTime = false;
            reTransition.hasFixedDuration = true;
            reTransition.duration = 0.0f;
            reTransition.conditions = new AnimatorCondition[]
            {
                new AnimatorCondition
                {
                    mode = AnimatorConditionMode.Equals,
                    parameter = pose.parameter,
                    threshold = pose.value
                }
            };
            
            // Preからリセットへの遷移
            var bypassTransition = poseState.AddTransition(defaultState);
            bypassTransition.canTransitionToSelf = false;
            bypassTransition.hasExitTime = false;
            bypassTransition.hasFixedDuration = true;
            bypassTransition.duration = 0.0f;
            bypassTransition.conditions = new AnimatorCondition[]
            {
                new AnimatorCondition
                {
                    mode = AnimatorConditionMode.NotEqual,
                    parameter = pose.parameter,
                    threshold = pose.value
                }
            };
        }

    }
}