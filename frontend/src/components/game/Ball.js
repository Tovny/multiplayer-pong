import { forwardRef } from "react";
import { BALL_RADIUS } from "../../constants/constants";

export const Ball = forwardRef(({ x, y }, ref) => {
  return (
    <div
      style={{
        width: `${BALL_RADIUS}%`,
        top: `${y}%`,
        left: `${x}%`,
        position: "absolute",
        backgroundColor: "white",
        aspectRatio: 1,
        borderRadius: "50%",
      }}
      className="PongBall"
      ref={ref}
    />
  );
});
