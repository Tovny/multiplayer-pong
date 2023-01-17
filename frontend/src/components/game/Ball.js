import { forwardRef } from "react";

export const Ball = forwardRef(({ x, y }, ref) => {
  return (
    <div
      style={{
        width: "30px",
        height: "30px",
        top: `${y}px`,
        left: `${x}px`,
        position: "absolute",
        backgroundColor: "white",
      }}
      className="PongBall"
      ref={ref}
    />
  );
});
