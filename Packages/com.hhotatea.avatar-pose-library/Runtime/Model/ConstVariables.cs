namespace com.hhotatea.avatar_pose_library.model
{
    public static class ConstVariables
    {
        /// <summary>
        /// パラメーター名
        /// </summary>
        public const string HeightParamPrefix = "AnimPoseHeight";
        public const string BaseParamPrefix = "AnimPoseBase";
        public const string HeadParamPrefix = "AnimPoseHead";
        public const string ArmParamPrefix = "AnimPoseArm";
        public const string FootParamPrefix = "AnimPoseFoot";
        public const string FingerParamPrefix = "AnimPoseFinger";
        public const string SpeedParamPrefix = "AnimPoseSpeed";
        public const string ResetParamPrefix = "AnimPoseReset";
        public const string MirrorParamPrefix = "AnimPoseMirror";
        // public const string BlockIdleParamPrefix = "AnimPoseBlock"; // 動的アニメーションかどうかのフラグ
        
        public const string AnimatorPrefix = "AnimPoseMotion";
        
        // 1つのIntパラメーターで管理するAnimationの最大数。
        public const int MaxAnimationState = 255;
    }
}
