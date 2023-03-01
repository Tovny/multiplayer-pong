import { forwardRef } from "react";
import { PADDLE_HEIGHT, PADDLE_WIDTH } from "../../constants/constants";

export const Paddle = forwardRef(
  ({ y, tabIndex = 0, position, isPlayer }, ref) => {
    return (
      <div
        role="button"
        className="Paddle"
        tabIndex={tabIndex}
        style={{
          width: `${PADDLE_WIDTH}%`,
          height: `${PADDLE_HEIGHT}%`,
          position: "absolute",
          backgroundColor: isPlayer ? "red" : "#ffffff",
          top: `${y}%`,
          left: position === "left" ? "0" : `${100 - PADDLE_WIDTH}%`,
          transition: "top 25ms linear",
        }}
        ref={ref}
      ></div>
    );
  }
);
