#pragma once

#include "CoreMinimal.h"
#include "GameFramework/Character.h"
#include "QHPlayerCharacter.generated.h"

class USpringArmComponent;
class UCameraComponent;
class UInputMappingContext;
class UInputAction;
struct FInputActionValue;

/**
 * Controllable athlete foundation.
 * Animation, ball possession and basketball actions are separate systems so
 * movement can be tuned without coupling it to ball simulation.
 */
UCLASS()
class QUBEHOOPS_API AQHPlayerCharacter : public ACharacter
{
    GENERATED_BODY()

public:
    AQHPlayerCharacter();

protected:
    virtual void BeginPlay() override;
    virtual void SetupPlayerInputComponent(UInputComponent* PlayerInputComponent) override;

    UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category="Camera")
    TObjectPtr<USpringArmComponent> CameraBoom;

    UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category="Camera")
    TObjectPtr<UCameraComponent> FollowCamera;

    UPROPERTY(EditDefaultsOnly, BlueprintReadOnly, Category="Input")
    TObjectPtr<UInputMappingContext> DefaultMappingContext;

    UPROPERTY(EditDefaultsOnly, BlueprintReadOnly, Category="Input")
    TObjectPtr<UInputAction> MoveAction;

    UPROPERTY(EditDefaultsOnly, BlueprintReadOnly, Category="Input")
    TObjectPtr<UInputAction> LookAction;

    UPROPERTY(EditDefaultsOnly, BlueprintReadOnly, Category="Input")
    TObjectPtr<UInputAction> JumpAction;

    void Move(const FInputActionValue& Value);
    void Look(const FInputActionValue& Value);
    void JumpStarted();
};
