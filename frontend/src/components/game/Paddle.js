import { forwardRef } from "react";
import { PADDLE_HEIGHT, PADDLE_WIDTH } from "../../constants/constants";

export const Paddle = forwardRef(({ x, y, tabIndex = 0, position }, ref) => {
  return (
    <div
      role="button"
      className="Paddle"
      tabIndex={tabIndex}
      style={{
        width: `${PADDLE_WIDTH}%`,
        height: `${PADDLE_HEIGHT}%`,
        position: "absolute",
        backgroundColor: "#ffffff",
        opacity: "0.7",
        top: `${y}%`,
        left: position === "left" ? "0" : "99.5%",
        transition: "top 25ms linear",
      }}
      ref={ref}
    ></div>
  );
});
