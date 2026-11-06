#pragma once

#include "CoreMinimal.h"
#include "Components/ActorComponent.h"
#include "QHBallPossessionComponent.generated.h"

class AQHBasketball;

UENUM(BlueprintType)
enum class EQHBallPossessionState : uint8
{
    Empty,
    Holding
};

/**
 * Server-authoritative starter possession system.
 * Add this component to AQHPlayerCharacter (or a Blueprint child).
 */
UCLASS(ClassGroup=(QubeHoops), meta=(BlueprintSpawnableComponent))
class QUBEHOOPS_API UQHBallPossessionComponent : public UActorComponent
{
    GENERATED_BODY()

public:
    UQHBallPossessionComponent();

    UPROPERTY(ReplicatedUsing=OnRep_CurrentBall, BlueprintReadOnly, Category="Basketball")
    TObjectPtr<AQHBasketball> CurrentBall;

    UPROPERTY(Replicated, BlueprintReadOnly, Category="Basketball")
    EQHBallPossessionState State = EQHBallPossessionState::Empty;

    UPROPERTY(EditDefaultsOnly, BlueprintReadOnly, Category="Basketball")
    FName HandSocketName = TEXT("hand_r");

    UPROPERTY(EditDefaultsOnly, BlueprintReadOnly, Category="Basketball")
    float PickupRange = 180.0f;

    UFUNCTION(BlueprintCallable, Category="Basketball")
    void RequestPickup(AQHBasketball* Ball);

    UFUNCTION(BlueprintCallable, Category="Basketball")
    void RequestRelease(FVector ReleaseVelocity);

protected:
    UFUNCTION(Server, Reliable)
    void ServerPickup(AQHBasketball* Ball);

    UFUNCTION(Server, Reliable)
    void ServerRelease(FVector_NetQuantize ReleaseVelocity);

    UFUNCTION()
    void OnRep_CurrentBall();

    void AttachBall(AQHBasketball* Ball);
    void ReleaseBall(const FVector& ReleaseVelocity);
    virtual void GetLifetimeReplicatedProps(TArray<FLifetimeProperty>& OutLifetimeProps) const override;
};
