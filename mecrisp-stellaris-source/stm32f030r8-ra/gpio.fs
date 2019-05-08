\ -------------------------------------------------------------------------
\  Initialize gpio peripheral clocks and pins
\ -------------------------------------------------------------------------
: gpio-init ( -- )
  \ Enable GPIOA peripheral clock and set pin PA5 as output
  \ (push-pull, low speed) for led
  %1 17 lshift RCC_AHBENR bis!
  \ $400 GPIOA_MODER bis!  \  enable PA5 output pp
  %01 10 lshift GPIOA_MODER bis!  \  enable PA5 output pp
  ;

\ -------------------------------------------------------------------------
\ Led tasks
\ -------------------------------------------------------------------------

: led1-init ( -- )
  \ Initialize led as off
  $20 GPIOA_ODR bic!
  ;

: led1-toggle ( -- )
  \ Toggle led
  $20 GPIOA_ODR xor!
  ;

: led-task ( -- )
  gpio-init
  led1-init
  begin
    led1-toggle
    50 ms-delay
    led1-toggle
    200 ms-delay
  key? until
  ;
