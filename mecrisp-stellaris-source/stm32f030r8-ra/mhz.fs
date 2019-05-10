\ vim: set ts=2 sw=2 expandtab :

$40021000 constant RCC
  RCC $00 + constant RCC_CR
  RCC $04 + constant RCC_CFGR
  \ RCC $08 + constant RCC_CIR
  \ RCC $0C + constant RCC_APB2RSTR
  RCC $10 + constant RCC_APB1RSTR
  RCC $14 + constant RCC_AHBENR
  \ RCC $18 + constant RCC_APB2ENR
  RCC $1C + constant RCC_APB1ENR
  \ RCC $20 + constant RCC_BDCR
  \ RCC $24 + constant RCC_CSR
  \ RCC $28 + constant RCC_AHBRSTR
  \ RCC $2C + constant RCC_CFGR2
  RCC $30 + constant RCC_CFGR3
  \ RCC $34 + constant RCC_CR2

$40022000 constant FLASH
  FLASH $0 + constant FLASH_ACR

$48000000 constant GPIOA
  GPIOA $00 + constant GPIOA_MODER
  \ GPIOA $04 + constant GPIOA_OTYPER
  GPIOA $08 + constant GPIOA_OSPEEDR
  \ GPIOA $0C + constant GPIOA_PUPDR
  \ GPIOA $10 + constant GPIOA_IDR
  GPIOA $14 + constant GPIOA_ODR
  GPIOA $18 + constant GPIOA_BSRR
  GPIOA $20 + constant GPIOA_AFRL
  GPIOA $24 + constant GPIOA_AFRH
  GPIOA $28 + constant GPIOA_BRR

$40004400 constant USART2
  USART2 $C + constant USART2_BRR

: +console_speed ( n -- )  115200 / USART2_BRR !  ;   \ expecting SYSCLK speed
: flash_prefetch? ( -- )  %1 5 lshift FLASH_ACR bit@ ;
: +flash_ws0 0 FLASH_ACR ! ;

\ MCO SYSCLK out on PA8
: +mco ( -- )
  %1111 24 lshift RCC_CFGR bic!     \ turn off MCO output
  %0100 24 lshift RCC_CFGR bis!     \ SYSCLK output
  %10 16 lshift GPIOA_MODER bis!    \ PA8 Alternate Function 0 0b10
  %11 16 lshift GPIOA_OSPEEDR bis!  \ PA8 Fast 
  ;
: -pll_clk %1 24 lshift RCC_CR bic! ;
: rcc-hsirdy? %1 1 lshift RCC_CR bit@ ;
: rcc-cfgr-sws? %11 2 lshift RCC_CFGR bit@ ;
: +rcc-hsion %1 RCC_CR bis! ; \ enable HSION
: +rcc-swshsi ( -- )
   %11 RCC_CFGR bic!          \ switch to HSI 
   begin rcc-cfgr-sws? while repeat
   ;

: +hseon ( -- )
  %1 18 lshift RCC_CR bis!              \ set RCC_CR_HSEBYP
  %1 16 lshift RCC_CR bis!              \ set RCC_CR_HSEON
  begin %1 17 lshift RCC_CR bit@ until  \ wait for HSEON
;

\ --------------------------------------------------------------------------
: +hsi_clk ( -- )
  +rcc-hsion
  begin rcc-hsirdy? until   \ wait for HSIRDY
  8000000 +console_speed
  +rcc-swshsi
  -pll_clk                  \ disable PLLON
  +flash_ws0
  ;

\ Set the main clock to 16/24/48 MHz, keep baud rate at 115200
\ nucleo-f030r8 has an external clock we can use with HSEBYP
: MHz ( n -- )
  +hsi_clk                          \ switch back to hsi to make changes

  dup case
    16 of %10000 endof  \ Zero flash wait states, PRFTBE enabled
    24 of %10000 endof  \ Zero flash wait states, PRFTBE enabled
    48 of %10001 endof  \ One flash wait state, PRFTBE enabled
    \ all other
          %10000        \ Zero wait state if invalid
  endcase FLASH_ACR !
  
  +hseon

  \ %1 18 lshift RCC_CR bis!              \ set RCC_CR_HSEBYP
  \ %1 16 lshift RCC_CR bis!              \ set RCC_CR_HSEON
  \ begin %1 17 lshift RCC_CR bit@ until  \ wait for HSEON

  %1    16 lshift RCC_CFGR bis! \ set RCC_CFGR_PLLSRC_HSE_PREDIV DIV/1
  %1111 18 lshift RCC_CFGR bic! \ clr RCC_CFGR_PLLMUL

  dup case
    16 of ( bic! set it to mul 2  )     endof \ PLL factor: 8 * 2 = 16 MHz
    24 of %0001 18 lshift RCC_CFGR bis! endof \ PLL factor: 8 * 3 = 24 MHz
    48 of %0100 18 lshift RCC_CFGR bis! endof \ PLL factor: 8 * 6 = 48 MHz
    \ all other
  endcase

  %1111 4 lshift RCC_CFGR bic!  \ HPRE DIV 1, HCLK = SYSCLK
  %111  8 lshift RCC_CFGR bic!  \ PPRE DIV 1, PCLK = HCLK

  %1 24 lshift RCC_CR bis!                \ set PLLON
  begin %1 25 lshift RCC_CR bit@ until    \ wait for PLLRDY

  %10 0 lshift RCC_CFGR bis!         \ Set RCC_CFGR_SW for PLL is system clock
  100 0 do loop

  ( pop the stack value ) case
    16 of 138 USART2_BRR ! endof    \ Set console baud rate to 16000000/115200
    24 of 208 USART2_BRR ! endof    \ Set console baud rate to 24000000/115200
    48 of 416 USART2_BRR ! endof    \ Set console baud rate to 48000000/115200
  \ all other
          138 USART2_BRR ! cr
          ." Error: Invalid MCLK! defaulting to 16 MHz" cr
          ." [ 16 | 24 | 48 ] are valid options" cr
  endcase
  ;

: 8MHz-hsi ( -- ) +hsi_clk 8000000 +console_speed ;
: 16MHz-hsebyp ( -- ) 16 MHz ;
: 24MHz-hsebyp ( -- ) 24 MHz ;
: 48MHz-hsebyp ( -- ) 48 MHz ;

: MHZ ( -- ) ( hide previous version ) ;

+mco

\ end mhz.fs functions: +mco, +hsi_clk, [16|24|48] MHz
