import { forwardRef } from "react";

export const Paddle = forwardRef(({ x, y, tabIndex = 0 }, ref) => {
  return (
    <div
      role="button"
      className="Paddle"
      tabIndex={tabIndex}
      style={{
        width: "15px",
        height: "150px",
        position: "absolute",
        backgroundColor: "#ffffff",
        opacity: "0.7",
        top: `${y}px`,
        left: `${x}px`,
        transition: "top 25ms linear",
      }}
      ref={ref}
    ></div>
  );
});
