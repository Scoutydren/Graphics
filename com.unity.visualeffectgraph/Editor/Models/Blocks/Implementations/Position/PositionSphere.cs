using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Block
{
    [VFXInfo(category = "Position", variantProvider = typeof(PositionBaseProvider))]
    class PositionSphere : PositionBase
    {
        public override string name { get { return string.Format(base.name, "Sphere"); } }

        public class InputProperties
        {
            [Tooltip("Sets the sphere used for positioning the particles.")]
            public ArcSphere ArcSphere = ArcSphere.defaultValue;
        }

        public class CustomProperties
        {
            [Range(0, 1), Tooltip("When using customized emission, control the position around the arc to emit particles from.")]
            public float ArcSequencer = 0.0f;
        }

        protected override bool needDirectionWrite => true;

        public override IEnumerable<VFXNamedExpression> parameters
        {
            get
            {
                var allSlots = GetExpressionsFromSlots(this);
                foreach (var p in allSlots.Where(e =>      e.name == "ArcSphere_arc"
                                                        || e.name == "ArcSequencer"
                                                        || e.name == "ArcSphere_sphere_radius"))
                    yield return p;

                var thickness = allSlots.FirstOrDefault(o => o.name == "Thickness").exp;
                var radius = allSlots.FirstOrDefault(o => o.name == "ArcSphere_sphere_radius").exp;
                var fakeRadius = allSlots.FirstOrDefault(o => o.name == "ArcSphere_arc").exp; //radius TODOPAUL : Isolate this fix, log an issue, there was a mistake, keep old behavior
                yield return new VFXNamedExpression(CalculateVolumeFactor(positionMode, fakeRadius, thickness), "volumeFactor");

                var eulerAngle = allSlots.FirstOrDefault(o => o.name == "ArcSphere_sphere_angles").exp;
                var center = allSlots.FirstOrDefault(o => o.name == "ArcSphere_sphere_center").exp;
                var zeroF3 = VFXOperatorUtility.ZeroExpression[VFXValueType.Float3];
                var oneF3 = VFXOperatorUtility.OneExpression[VFXValueType.Float3];

                VFXExpression rotationMatrix = new VFXExpressionTRSToMatrix(zeroF3, eulerAngle, oneF3);
                VFXExpression i = new VFXExpressionMatrixToVector3s(rotationMatrix, VFXValue.Constant(0));
                VFXExpression j = new VFXExpressionMatrixToVector3s(rotationMatrix, VFXValue.Constant(1));
                VFXExpression k = new VFXExpressionMatrixToVector3s(rotationMatrix, VFXValue.Constant(2));

                var transformMatrix = new VFXExpressionVector3sToMatrix(i, j, k, center);
                yield return new VFXNamedExpression(transformMatrix, "transformMatrix");
            }
        }

        public override string source
        {
            get
            {
                string outSource = @"float cosPhi = 2.0f * RAND - 1.0f;";
                if (spawnMode == SpawnMode.Random)
                    outSource += @"float theta = ArcSphere_arc * RAND;";
                else
                    outSource += @"float theta = ArcSphere_arc * ArcSequencer;";

                outSource += @"
float rNorm = pow(volumeFactor + (1 - volumeFactor) * RAND, 1.0f / 3.0f);

float2 sincosTheta;
sincos(theta, sincosTheta.x, sincosTheta.y);
sincosTheta *= sqrt(1.0f - cosPhi * cosPhi);
float3 finalDir = float3(sincosTheta, cosPhi);
float3 finalPos = float3(sincosTheta, cosPhi) * rNorm * ArcSphere_sphere_radius;
finalPos = mul(transformMatrix, float4(finalPos, 1.0f));
finalDir = mul((float3x3)transformMatrix, finalDir);
";

                outSource += string.Format(composeDirectionFormatString, "finalDir") + "\n";
                outSource += string.Format(composePositionFormatString, "finalPos");

                return outSource;
            }
        }
    }
}
