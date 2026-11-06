#pragma once

#include "CoreMinimal.h"
#include "GameFramework/Actor.h"
#include "QHBasketball.generated.h"

class USphereComponent;
class UStaticMeshComponent;

/**
 * Replicated, physics-driven basketball.
 * Assign a basketball-shaped Static Mesh in a Blueprint subclass; the C++ actor
 * supplies collision, rigid-body simulation, and possession state.
 */
UCLASS()
class QUBEHOOPS_API AQHBasketball : public AActor
{
    GENERATED_BODY()

public:
    AQHBasketball();

    UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category="Basketball")
    TObjectPtr<USphereComponent> Collision;

    UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category="Basketball")
    TObjectPtr<UStaticMeshComponent> BallMesh;

    UPROPERTY(ReplicatedUsing=OnRep_IsHeld, BlueprintReadOnly, Category="Basketball")
    bool bIsHeld = false;

    UFUNCTION(BlueprintCallable, Category="Basketball")
    bool IsAvailable() const { return !bIsHeld; }

    /** Intended to be called by the authoritative possession component. */
    void SetHeldState(bool bNewHeld);

    /** Restore a loose ball to a known location and velocity. */
    UFUNCTION(BlueprintCallable, Category="Basketball")
    void ResetBall(const FTransform& SpawnTransform);

protected:
    UFUNCTION()
    void OnRep_IsHeld();

    virtual void GetLifetimeReplicatedProps(TArray<FLifetimeProperty>& OutLifetimeProps) const override;
};
